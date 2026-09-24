Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models

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

        Public Sub New(repository As InterruttoreRepository)
            _repository = repository
            _catalogoConfigurazioni = _repository.ElencoConfigurazioniCatalogo()
            _configurazioniPendenti = New List(Of ConfigurazioneInterruttore)

            Marche = New ObservableCollection(Of Marca)(_repository.ElencoMarche())
            Serie = New ObservableCollection(Of Serie)(_repository.ElencoSerie())
            Tipi = New ObservableCollection(Of TipoInterruttore)(_repository.ElencoTipi())
            LineeProdottoCatalogo = New ObservableCollection(Of LineaProdotto)(_repository.ElencoLineeProdotto())
            Interruttori = New ObservableCollection(Of Interruttore)

            TaglieAssegnate = New ObservableCollection(Of TagliaRiga)
            LineeProdottoAssegnate = New ObservableCollection(Of LineaProdotto)
            ConfigurazioniAssegnate = New ObservableCollection(Of RigaConfigurazioneAssegnata)
            CelleAssegnate = New ObservableCollection(Of CellaRiga)

            NuovoCommand = New RelayCommand(AddressOf EseguiNuovo)
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

        Private _interruttoreSelezionato As Interruttore
        Public Property InterruttoreSelezionato As Interruttore
            Get
                Return _interruttoreSelezionato
            End Get
            Set(value As Interruttore)
                _interruttoreSelezionato = value
                OnPropertyChanged()
                CaricaDettaglio()
                AggiornaCanExecuteComandi()
            End Set
        End Property

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

#Region "Sezione: Configurazione cella (sola lettura, maschera dedicata ancora da fare)"

        Public ReadOnly Property ApriConfigurazioneCellaCommand As RelayCommand
        Public ReadOnly Property CelleAssegnate As ObservableCollection(Of CellaRiga)

        ''' <summary>Sollevato quando l'utente chiede di aprire la maschera Configurazione cella.</summary>
        Public Event RichiestaAperturaConfigurazioneCella As EventHandler

        Public Sub RicaricaCelleAssegnate()
            CelleAssegnate.Clear()
            If _id = 0 Then Return
            For Each c In _repository.CelleDiInterruttore(_id)
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

        Private Sub EseguiNuovo()
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
            CelleAssegnate.Clear()
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub CaricaDettaglio()
            If InterruttoreSelezionato Is Nothing Then
                Return
            End If

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
            RicaricaCelleAssegnate()

            ' Le linee prodotto assegnate si deducono dalle celle già salvate per questo interruttore;
            ' l'utente può comunque aggiungerne altre prima di aprire la maschera Configurazione cella.
            LineeProdottoAssegnate.Clear()
            For Each idLinea In CelleAssegnate.Select(Function(c) c.IdLineaProdotto).Distinct()
                Dim linea = LineeProdottoCatalogo.FirstOrDefault(Function(l) l.Id = idLinea)
                If linea IsNot Nothing Then LineeProdottoAssegnate.Add(linea)
            Next
        End Sub

        ''' <summary>
        ''' Per salvare un interruttore serve tutto completo: anagrafica base, almeno una
        ''' Configurazione, almeno una Taglia e almeno una Linea prodotto.
        ''' </summary>
        Private Function PuoSalvare() As Boolean
            Return Not String.IsNullOrWhiteSpace(Nome) AndAlso MarcaSelezionata IsNot Nothing AndAlso
                   SerieSelezionata IsNot Nothing AndAlso TipoSelezionato IsNot Nothing AndAlso NumeroPoli.HasValue AndAlso
                   _configurazioniPendenti.Count > 0 AndAlso TaglieAssegnate.Count > 0 AndAlso LineeProdottoAssegnate.Count > 0
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

            RicaricaElenco()
            InterruttoreSelezionato = Interruttori.FirstOrDefault(Function(i) i.Id = _id)
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiElimina()
            If InterruttoreSelezionato Is Nothing Then Return
            Dim conferma = MessageBox.Show(
                $"Eliminare definitivamente l'interruttore ""{InterruttoreSelezionato.Nome}""?",
                "Conferma eliminazione", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            If conferma <> MessageBoxResult.Yes Then Return

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
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiRimuoviTaglia()
            If TagliaSelezionata Is Nothing Then Return
            TaglieAssegnate.Remove(TagliaSelezionata)
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiAggiungiLineaProdotto()
            If LineaProdottoDaAggiungere Is Nothing Then Return
            If LineeProdottoAssegnate.Any(Function(l) l.Id = LineaProdottoDaAggiungere.Id) Then Return

            LineeProdottoAssegnate.Add(LineaProdottoDaAggiungere)
            LineaProdottoDaAggiungere = Nothing
            OnPropertyChanged(NameOf(LineaProdottoDaAggiungere))
            AggiornaCanExecuteComandi()
        End Sub

        Private Sub EseguiRimuoviLineaProdotto()
            If LineaProdottoAssegnataSelezionata Is Nothing Then Return
            LineeProdottoAssegnate.Remove(LineaProdottoAssegnataSelezionata)
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
