Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows
Imports System.Windows.Data
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models
Imports GestioneInterruttori.Views

Namespace ViewModels
    ''' <summary>
    ''' ViewModel della maschera di gestione anagrafica interruttori (inserimento/modifica/eliminazione),
    ''' riservata agli utenti UT DKC.
    ''' </summary>
    Public Class GestioneInterruttoreViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _repository As InterruttoreRepository
        Private ReadOnly _catalogoConfigurazioni As List(Of Configurazione)
        Private _id As Integer

        ''' <summary>
        ''' Configurazioni scelte per l'interruttore corrente, tenute in memoria: Configurazione interruttore
        ''' si può compilare prima ancora che l'interruttore sia stato salvato. Diventano definitive solo
        ''' quando si preme Salva sulla maschera principale.
        ''' </summary>
        Private _configurazioniPendenti As List(Of ConfigurazioneInterruttore)

        ''' <summary>Celle scelte per l'interruttore corrente, tenute in memoria con la stessa logica
        ''' di <see cref="_configurazioniPendenti"/>.</summary>
        Private _cellePendenti As List(Of ConfigurazioneCella)

        ''' <summary>True se ci sono modifiche non ancora salvate sull'interruttore corrente.</summary>
        Private _modificheNonSalvate As Boolean

        ''' <summary>True mentre si sta caricando/azzerando lo stato (CaricaDettaglio/EseguiNuovo):
        ''' in quella finestra le property setter non devono segnare "modifica utente".</summary>
        Private _stoCaricando As Boolean

        Public Sub New(repository As InterruttoreRepository)
            _repository = repository
            _catalogoConfigurazioni = _repository.ElencoConfigurazioniCatalogo()
            _configurazioniPendenti = New List(Of ConfigurazioneInterruttore)
            _cellePendenti = New List(Of ConfigurazioneCella)

            Marche = New ObservableCollection(Of Marca)(_repository.ElencoMarche())
            Serie = New ObservableCollection(Of Serie)(_repository.ElencoSerie())
            Tipi = New ObservableCollection(Of TipoInterruttore)(_repository.ElencoTipi())
            LineeProdottoCatalogo = New ObservableCollection(Of LineaProdotto)(_repository.ElencoLineeProdotto())
            Interruttori = New ObservableCollection(Of Interruttore)
            VistaInterruttori = CollectionViewSource.GetDefaultView(Interruttori)
            VistaInterruttori.Filter = AddressOf FiltraInterruttore

            TaglieAssegnate = New ObservableCollection(Of TagliaRiga)
            LineeProdottoAssegnate = New ObservableCollection(Of LineaProdotto)
            ConfigurazioniAssegnate = New ObservableCollection(Of RigaConfigurazioneAssegnata)
            CelleAssegnate = New ObservableCollection(Of CellaRiga)

            NuovoCommand = New RelayCommand(AddressOf EseguiRichiestaNuovo)
            SalvaCommand = New RelayCommand(AddressOf EseguiSalva, Function() PuoSalvare())
            EliminaCommand = New RelayCommand(AddressOf EseguiElimina, Function() InterruttoreSelezionato IsNot Nothing)

            AggiungiTagliaCommand = New RelayCommand(AddressOf EseguiAggiungiTaglia, Function() Not String.IsNullOrWhiteSpace(NuovaTaglia))
            RimuoviTagliaCommand = New RelayCommand(AddressOf EseguiRimuoviTaglia, Function() TagliaSelezionata IsNot Nothing)

            AggiungiLineaProdottoCommand = New RelayCommand(AddressOf EseguiAggiungiLineaProdotto, Function() LineaProdottoDaAggiungere IsNot Nothing)
            RimuoviLineaProdottoCommand = New RelayCommand(AddressOf EseguiRimuoviLineaProdotto, Function() LineaProdottoAssegnataSelezionata IsNot Nothing)

            ' Nessun vincolo: si può compilare Configurazione interruttore prima ancora di salvare l'anagrafica.
            ApriConfigurazioneInterruttoreCommand = New RelayCommand(
                Sub() RaiseEvent RichiestaAperturaConfigurazioneInterruttore(Me, EventArgs.Empty))

            ApriConfigurazioneCellaCommand = New RelayCommand(
                Sub() RaiseEvent RichiestaAperturaConfigurazioneCella(Me, EventArgs.Empty),
                Function() TaglieAssegnate.Count > 0 AndAlso LineeProdottoAssegnate.Count > 0)

            RicaricaElenco()
            EseguiNuovo() ' stato iniziale pulito: RadiceDwg/Nome partono da "" e non da Nothing
        End Sub

#Region "Elenchi di supporto per le combobox"

        Public ReadOnly Property Marche As ObservableCollection(Of Marca)
        Public ReadOnly Property Serie As ObservableCollection(Of Serie)
        Public ReadOnly Property Tipi As ObservableCollection(Of TipoInterruttore)
        Public ReadOnly Property LineeProdottoCatalogo As ObservableCollection(Of LineaProdotto)

#End Region

#Region "Elenco interruttori (master)"

        Public ReadOnly Property Interruttori As ObservableCollection(Of Interruttore)
        Public ReadOnly Property VistaInterruttori As ICollectionView

        Private _testoFiltro As String = ""
        Public Property TestoFiltro As String
            Get
                Return _testoFiltro
            End Get
            Set(value As String)
                _testoFiltro = value
                OnPropertyChanged()
                VistaInterruttori.Refresh()
            End Set
        End Property

        Private Function FiltraInterruttore(elemento As Object) As Boolean
            If String.IsNullOrWhiteSpace(TestoFiltro) Then Return True
            Dim interruttore = TryCast(elemento, Interruttore)
            Return interruttore IsNot Nothing AndAlso
                interruttore.Nome IsNot Nothing AndAlso
                interruttore.Nome.Contains(TestoFiltro, StringComparison.OrdinalIgnoreCase)
        End Function

        Private _interruttoreSelezionato As Interruttore
        Public Property InterruttoreSelezionato As Interruttore
            Get
                Return _interruttoreSelezionato
            End Get
            Set(value As Interruttore)
                If ReferenceEquals(value, _interruttoreSelezionato) Then Return

                If Not ConfermaAbbandonoModifiche() Then
                    ' L'utente ha annullato il cambio: ripristina la selezione precedente nella UI
                    ' (il DataGrid è già passato visivamente al nuovo valore, va corretto).
                    OnPropertyChanged(NameOf(InterruttoreSelezionato))
                    Return
                End If

                _interruttoreSelezionato = value
                OnPropertyChanged()
                CaricaDettaglio()
                AggiornaCanExecuteComandi()
            End Set
        End Property

#End Region

#Region "Modifiche non salvate"

        ''' <summary>Usato dalla finestra per decidere se avvisare alla chiusura.</summary>
        Public ReadOnly Property CiSonoModificheNonSalvate As Boolean
            Get
                Return _modificheNonSalvate
            End Get
        End Property

        ''' <summary>
        ''' Se ci sono modifiche non salvate, chiede all'utente cosa fare. Restituisce True se si può
        ''' procedere con l'operazione che ha innescato il controllo (cambio interruttore, nuovo, chiusura),
        ''' False se l'utente ha annullato.
        ''' </summary>
        Public Function ConfermaAbbandonoModifiche() As Boolean
            If Not _modificheNonSalvate Then Return True

            Dim esito = DialogModificheNonSalvate.Chiedi(Application.Current?.MainWindow, "Ci sono modifiche non salvate su questo interruttore. Vuoi salvarle prima di continuare?")

            Select Case esito
                Case MessageBoxResult.Cancel
                    Return False
                Case MessageBoxResult.Yes
                    If Not PuoSalvare() Then
                        MessageBox.Show(
                            "Completa prima tutti i campi obbligatori per salvare (anagrafica, configurazione, almeno una taglia, una linea prodotto e una cella).",
                            "Impossibile salvare", MessageBoxButton.OK, MessageBoxImage.Information)
                        Return False
                    End If
                    EseguiSalva()
                    Return True
                Case Else ' No: scarta le modifiche e procede
                    _modificheNonSalvate = False
                    Return True
            End Select
        End Function

        Private Sub SegnaModificato()
            If _stoCaricando Then Return
            _modificheNonSalvate = True
        End Sub

#End Region

#Region "Sezione: Anagrafica base"

        Private _marcaSelezionata As Marca
        Public Property MarcaSelezionata As Marca
            Get
                Return _marcaSelezionata
            End Get
            Set(value As Marca)
                _marcaSelezionata = value
                OnPropertyChanged()
                SegnaModificato()
                DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _serieSelezionata As Serie
        Public Property SerieSelezionata As Serie
            Get
                Return _serieSelezionata
            End Get
            Set(value As Serie)
                _serieSelezionata = value
                OnPropertyChanged()
                SegnaModificato()
                DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _tipoSelezionato As TipoInterruttore
        Public Property TipoSelezionato As TipoInterruttore
            Get
                Return _tipoSelezionato
            End Get
            Set(value As TipoInterruttore)
                _tipoSelezionato = value
                OnPropertyChanged()
                SegnaModificato()
                DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _nome As String
        Public Property Nome As String
            Get
                Return _nome
            End Get
            Set(value As String)
                _nome = value
                OnPropertyChanged()
                SegnaModificato()
                DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _numeroPoli As Integer?
        Public Property NumeroPoli As Integer?
            Get
                Return _numeroPoli
            End Get
            Set(value As Integer?)
                _numeroPoli = value
                OnPropertyChanged()
                SegnaModificato()
                DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _radiceDwg As String
        Public Property RadiceDwg As String
            Get
                Return _radiceDwg
            End Get
            Set(value As String)
                _radiceDwg = value
                OnPropertyChanged()
                SegnaModificato()
            End Set
        End Property

        Public ReadOnly Property NumeriPoli As Integer() = {1, 2, 3, 4}

#End Region

#Region "Sezione: Configurazione interruttore (in memoria fino al Salva finale)"

        Public ReadOnly Property ConfigurazioniAssegnate As ObservableCollection(Of RigaConfigurazioneAssegnata)
        Public ReadOnly Property ApriConfigurazioneInterruttoreCommand As RelayCommand

        ''' <summary>Sollevato quando l'utente chiede di aprire la maschera Configurazione interruttore.</summary>
        Public Event RichiestaAperturaConfigurazioneInterruttore As EventHandler

        ''' <summary>Le configurazioni scelte finora (in memoria), da passare alla finestra dedicata.</summary>
        Public ReadOnly Property ConfigurazioniPendenti As List(Of ConfigurazioneInterruttore)
            Get
                Return _configurazioniPendenti
            End Get
        End Property

        ''' <summary>Chiamato dopo la chiusura (confermata) della finestra Configurazione interruttore.</summary>
        Public Sub AggiornaConfigurazioniPendenti(nuoveConfigurazioni As List(Of ConfigurazioneInterruttore))
            _configurazioniPendenti = If(nuoveConfigurazioni, New List(Of ConfigurazioneInterruttore))
            RicostruisciConfigurazioniAssegnate()
            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub RicostruisciConfigurazioniAssegnate()
            ConfigurazioniAssegnate.Clear()
            For Each c In _configurazioniPendenti
                Dim catalogo = _catalogoConfigurazioni.FirstOrDefault(Function(k) k.Id = c.IdConfigurazione)
                If catalogo Is Nothing Then Continue For
                ConfigurazioniAssegnate.Add(New RigaConfigurazioneAssegnata With {
                    .Tipo = catalogo.Tipo,
                    .Nome = catalogo.Configurazione,
                    .DeltaH = c.DeltaH,
                    .DeltaL = c.DeltaL,
                    .DeltaP = c.DeltaP
                })
            Next
        End Sub

#End Region

#Region "Sezione: Taglie e potenza dissipata"

        Public ReadOnly Property TaglieAssegnate As ObservableCollection(Of TagliaRiga)

        Private _tagliaSelezionata As TagliaRiga
        Public Property TagliaSelezionata As TagliaRiga
            Get
                Return _tagliaSelezionata
            End Get
            Set(value As TagliaRiga)
                _tagliaSelezionata = value
                OnPropertyChanged()
                DirectCast(RimuoviTagliaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _nuovaTaglia As String
        Public Property NuovaTaglia As String
            Get
                Return _nuovaTaglia
            End Get
            Set(value As String)
                _nuovaTaglia = value
                OnPropertyChanged()
                DirectCast(AggiungiTagliaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Public Property NuovaPotenzaDissipata As Double?

        Public ReadOnly Property AggiungiTagliaCommand As RelayCommand
        Public ReadOnly Property RimuoviTagliaCommand As RelayCommand

#End Region

#Region "Sezione: Linee prodotto (transitorio, base per Configurazione Cella)"

        Public ReadOnly Property LineeProdottoAssegnate As ObservableCollection(Of LineaProdotto)

        Private _lineaProdottoAssegnataSelezionata As LineaProdotto
        Public Property LineaProdottoAssegnataSelezionata As LineaProdotto
            Get
                Return _lineaProdottoAssegnataSelezionata
            End Get
            Set(value As LineaProdotto)
                _lineaProdottoAssegnataSelezionata = value
                OnPropertyChanged()
                DirectCast(RimuoviLineaProdottoCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Private _lineaProdottoDaAggiungere As LineaProdotto
        Public Property LineaProdottoDaAggiungere As LineaProdotto
            Get
                Return _lineaProdottoDaAggiungere
            End Get
            Set(value As LineaProdotto)
                _lineaProdottoDaAggiungere = value
                OnPropertyChanged()
                DirectCast(AggiungiLineaProdottoCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property AggiungiLineaProdottoCommand As RelayCommand
        Public ReadOnly Property RimuoviLineaProdottoCommand As RelayCommand

#End Region

#Region "Sezione: Configurazione cella (in memoria fino al Salva finale)"

        Public ReadOnly Property ApriConfigurazioneCellaCommand As RelayCommand
        Public ReadOnly Property CelleAssegnate As ObservableCollection(Of CellaRiga)

        ''' <summary>Sollevato quando l'utente chiede di aprire la maschera Configurazione cella.</summary>
        Public Event RichiestaAperturaConfigurazioneCella As EventHandler

        ''' <summary>Le celle scelte finora (in memoria), da passare alla finestra dedicata.</summary>
        Public ReadOnly Property CellePendenti As List(Of ConfigurazioneCella)
            Get
                Return _cellePendenti
            End Get
        End Property

        ''' <summary>Chiamato dopo la chiusura (confermata) della finestra Configurazione cella.</summary>
        Public Sub AggiornaCellePendenti(nuoveCelle As List(Of ConfigurazioneCella))
            _cellePendenti = If(nuoveCelle, New List(Of ConfigurazioneCella))
            RicostruisciCelleAssegnate()
            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub RicostruisciCelleAssegnate()
            CelleAssegnate.Clear()
            For Each c In _cellePendenti
                CelleAssegnate.Add(New CellaRiga With {
                    .IdLineaProdotto = c.IdLineaProdotto,
                    .LineaProdotto = LineeProdottoCatalogo.FirstOrDefault(Function(l) l.Id = c.IdLineaProdotto)?.Nome,
                    .Taglia = c.Taglia,
                    .AltezzaCella = c.AltezzaCella,
                    .LarghezzaCella = c.LarghezzaCella,
                    .AltezzaCellaVerticale = c.AltezzaCellaVerticale,
                    .LarghezzaCellaVerticale = c.LarghezzaCellaVerticale,
                    .ProfonditaCella = c.ProfonditaCella
                })
            Next
        End Sub

#End Region

#Region "Comandi principali"

        Public ReadOnly Property NuovoCommand As RelayCommand
        Public ReadOnly Property SalvaCommand As RelayCommand
        Public ReadOnly Property EliminaCommand As RelayCommand

#End Region

#Region "Logica"

        Private Sub RicaricaElenco()
            Interruttori.Clear()
            For Each i In _repository.ElencoInterruttori()
                Interruttori.Add(i)
            Next
        End Sub

        ''' <summary>Gestore del comando Nuovo: chiede conferma se ci sono modifiche in sospeso.</summary>
        Private Sub EseguiRichiestaNuovo()
            If Not ConfermaAbbandonoModifiche() Then Return
            EseguiNuovo()
        End Sub

        Private Sub EseguiNuovo()
            _stoCaricando = True
            _modificheNonSalvate = False ' evita che l'assegnazione sotto rientri in ConfermaAbbandonoModifiche
            InterruttoreSelezionato = Nothing
            _id = 0
            MarcaSelezionata = Nothing
            SerieSelezionata = Nothing
            TipoSelezionato = Nothing
            Nome = String.Empty
            NumeroPoli = Nothing
            RadiceDwg = String.Empty
            TaglieAssegnate.Clear()
            LineeProdottoAssegnate.Clear()
            _configurazioniPendenti = New List(Of ConfigurazioneInterruttore)
            ConfigurazioniAssegnate.Clear()
            _cellePendenti = New List(Of ConfigurazioneCella)
            CelleAssegnate.Clear()
            _stoCaricando = False
            _modificheNonSalvate = False
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub CaricaDettaglio()
            If InterruttoreSelezionato Is Nothing Then
                Return
            End If

            _stoCaricando = True
            _id = InterruttoreSelezionato.Id
            MarcaSelezionata = Marche.FirstOrDefault(Function(m) m.Id = InterruttoreSelezionato.IdMarca)
            SerieSelezionata = Serie.FirstOrDefault(Function(s) s.Id = InterruttoreSelezionato.IdSerie)
            TipoSelezionato = Tipi.FirstOrDefault(Function(t) t.Id = InterruttoreSelezionato.IdTipo)
            Nome = InterruttoreSelezionato.Nome
            NumeroPoli = InterruttoreSelezionato.NumeroPoli
            RadiceDwg = InterruttoreSelezionato.RadiceDwg

            TaglieAssegnate.Clear()
            For Each t In _repository.TaglieDiInterruttore(_id)
                TaglieAssegnate.Add(New TagliaRiga With {.Taglia = t.Taglia, .PotenzaDissipata = t.PotenzaDissipata})
            Next

            _configurazioniPendenti = _repository.ConfigurazioniDiInterruttore(_id)
            RicostruisciConfigurazioniAssegnate()

            _cellePendenti = _repository.CelleDiInterruttore(_id)
            RicostruisciCelleAssegnate()

            ' Le linee prodotto assegnate si deducono dalle celle già salvate per questo interruttore;
            ' l'utente può comunque aggiungerne altre prima di aprire la maschera Configurazione cella.
            LineeProdottoAssegnate.Clear()
            For Each idLinea In _cellePendenti.Select(Function(c) c.IdLineaProdotto).Distinct()
                Dim linea = LineeProdottoCatalogo.FirstOrDefault(Function(l) l.Id = idLinea)
                If linea IsNot Nothing Then LineeProdottoAssegnate.Add(linea)
            Next

            _stoCaricando = False
            _modificheNonSalvate = False
        End Sub

        ''' <summary>
        ''' Per salvare un interruttore serve tutto completo: anagrafica base, almeno una
        ''' Configurazione, almeno una Taglia, almeno una Linea prodotto e almeno una Cella.
        ''' </summary>
        Private Function PuoSalvare() As Boolean
            Return Not String.IsNullOrWhiteSpace(Nome) AndAlso MarcaSelezionata IsNot Nothing AndAlso
                   SerieSelezionata IsNot Nothing AndAlso TipoSelezionato IsNot Nothing AndAlso NumeroPoli.HasValue AndAlso
                   _configurazioniPendenti.Count > 0 AndAlso TaglieAssegnate.Count > 0 AndAlso
                   LineeProdottoAssegnate.Count > 0 AndAlso _cellePendenti.Count > 0
        End Function

        Private Sub EseguiSalva()
            Dim interruttore As New Interruttore With {
                .Id = _id,
                .IdMarca = MarcaSelezionata.Id,
                .IdSerie = SerieSelezionata.Id,
                .Nome = Nome,
                .IdTipo = TipoSelezionato.Id,
                .NumeroPoli = NumeroPoli.Value,
                .RadiceDwg = RadiceDwg
            }

            Dim taglieValide = TaglieAssegnate.
                Where(Function(t) Not String.IsNullOrWhiteSpace(t.Taglia)).
                Select(Function(t) New TagliaInterruttore With {
                    .IdInterruttore = _id,
                    .Taglia = t.Taglia,
                    .PotenzaDissipata = t.PotenzaDissipata
                })

            _id = _repository.Salva(interruttore, taglieValide)

            Dim configurazioniDaSalvare = _configurazioniPendenti.
                Select(Function(c) New ConfigurazioneInterruttore With {
                    .IdInterruttore = _id,
                    .IdConfigurazione = c.IdConfigurazione,
                    .DeltaH = c.DeltaH,
                    .DeltaL = c.DeltaL,
                    .DeltaP = c.DeltaP
                })
            _repository.SalvaConfigurazioni(_id, configurazioniDaSalvare)

            Dim celleDaSalvare = _cellePendenti.
                Select(Function(c) New ConfigurazioneCella With {
                    .IdLineaProdotto = c.IdLineaProdotto,
                    .IdInterruttore = _id,
                    .Taglia = c.Taglia,
                    .AltezzaCella = c.AltezzaCella,
                    .LarghezzaCella = c.LarghezzaCella,
                    .AltezzaCellaVerticale = c.AltezzaCellaVerticale,
                    .LarghezzaCellaVerticale = c.LarghezzaCellaVerticale,
                    .ProfonditaCella = c.ProfonditaCella
                })
            _repository.SalvaCelle(_id, celleDaSalvare)

            _modificheNonSalvate = False ' evita di rientrare in ConfermaAbbandonoModifiche sotto
            RicaricaElenco()
            InterruttoreSelezionato = Interruttori.FirstOrDefault(Function(i) i.Id = _id)
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiElimina()
            If InterruttoreSelezionato Is Nothing Then Return
            If Not DialogConferma.Chiedi(Application.Current?.MainWindow, $"Eliminare definitivamente l'interruttore ""{InterruttoreSelezionato.Nome}""?", "Conferma eliminazione", "Elimina") Then Return

            _repository.Elimina(InterruttoreSelezionato.Id)
            RicaricaElenco()
            EseguiNuovo()
        End Sub

        Private Sub EseguiAggiungiTaglia()
            If String.IsNullOrWhiteSpace(NuovaTaglia) Then Return
            If TaglieAssegnate.Any(Function(t) String.Equals(t.Taglia, NuovaTaglia, StringComparison.OrdinalIgnoreCase)) Then Return

            TaglieAssegnate.Add(New TagliaRiga With {.Taglia = NuovaTaglia, .PotenzaDissipata = NuovaPotenzaDissipata})
            NuovaTaglia = String.Empty
            NuovaPotenzaDissipata = Nothing
            OnPropertyChanged(NameOf(NuovaPotenzaDissipata))
            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiRimuoviTaglia()
            If TagliaSelezionata Is Nothing Then Return
            If Not DialogConferma.Chiedi(Application.Current?.MainWindow, $"Rimuovere la taglia ""{TagliaSelezionata.Taglia}"" da questo interruttore?") Then Return

            Dim tagliaRimossa = TagliaSelezionata.Taglia
            TaglieAssegnate.Remove(TagliaSelezionata)

            ' Le celle già assegnate a quella taglia non hanno più senso: le tolgo dalle celle
            ' pendenti (un'assegnazione che copriva anche altre taglie resta per quelle restanti;
            ' un'assegnazione che copriva solo questa taglia sparisce del tutto).
            _cellePendenti = _cellePendenti.Where(Function(c) c.Taglia <> tagliaRimossa).ToList()
            RicostruisciCelleAssegnate()

            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiAggiungiLineaProdotto()
            If LineaProdottoDaAggiungere Is Nothing Then Return
            If LineeProdottoAssegnate.Any(Function(l) l.Id = LineaProdottoDaAggiungere.Id) Then Return

            LineeProdottoAssegnate.Add(LineaProdottoDaAggiungere)
            LineaProdottoDaAggiungere = Nothing
            OnPropertyChanged(NameOf(LineaProdottoDaAggiungere))
            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiRimuoviLineaProdotto()
            If LineaProdottoAssegnataSelezionata Is Nothing Then Return
            If Not DialogConferma.Chiedi(Application.Current?.MainWindow, $"Rimuovere la linea prodotto ""{LineaProdottoAssegnataSelezionata.Nome}"" da questo interruttore?") Then Return

            Dim idLineaRimossa = LineaProdottoAssegnataSelezionata.Id
            LineeProdottoAssegnate.Remove(LineaProdottoAssegnataSelezionata)

            ' Stessa logica di EseguiRimuoviTaglia, mirror per Linea prodotto.
            _cellePendenti = _cellePendenti.Where(Function(c) c.IdLineaProdotto <> idLineaRimossa).ToList()
            RicostruisciCelleAssegnate()

            SegnaModificato()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub AggiornaCanExecuteComandi()
            DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
            DirectCast(EliminaCommand, RelayCommand).RaiseCanExecuteChanged()
            DirectCast(ApriConfigurazioneCellaCommand, RelayCommand).RaiseCanExecuteChanged()
        End Sub

#End Region

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(<CallerMemberName> Optional nomeProprieta As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(nomeProprieta))
        End Sub
    End Class
End Namespace
