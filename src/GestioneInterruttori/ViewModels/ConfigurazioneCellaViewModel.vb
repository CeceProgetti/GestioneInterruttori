Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows.Threading
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' ViewModel della sola sezione "Configurazione cella".
    ''' Due modalità, come per ConfigurazioneInterruttoreViewModel:
    ''' - DB (idInterruttore valido): legge/scrive direttamente su ConfigurazioneCella.
    ''' - In memoria (nessun idInterruttore): lavora su una lista passata dal chiamante e la
    '''   restituisce alla chiusura, senza toccare il DB.
    ''' Una taglia o linea prodotto già usata in un'assegnazione non è più selezionabile
    ''' per crearne una nuova, finché non viene tolta da quella assegnazione.
    ''' </summary>
    Public Class ConfigurazioneCellaViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _repository As InterruttoreRepository
        Private ReadOnly _idInterruttore As Integer?
        Private _timerConferma As DispatcherTimer

        Private _tutteLeTaglie As List(Of String)
        Private _tutteLeLineeProdotto As List(Of LineaProdotto)

        ''' <summary>Istantanea dei Gruppi presa all'apertura e dopo ogni salvataggio, per capire
        ''' se ci sono modifiche non salvate alla chiusura.</summary>
        Private _firmaSalvata As HashSet(Of String)

        ''' <summary>Modalità DB: interruttore già salvato.</summary>
        Public Sub New(repository As InterruttoreRepository, idInterruttore As Integer)
            _repository = repository
            _idInterruttore = idInterruttore

            Inizializza()

            Dim interruttore = _repository.OttieniInterruttore(idInterruttore)
            NomeInterruttore = If(interruttore?.Nome, "—")

            _tutteLeTaglie = _repository.TaglieDiInterruttore(idInterruttore).Select(Function(t) t.Taglia).ToList()
            _tutteLeLineeProdotto = _repository.ElencoLineeProdotto()
            Dim celleEsistenti = _repository.CelleDiInterruttore(idInterruttore)

            CaricaGruppiIniziali(celleEsistenti, _tutteLeLineeProdotto)
            AggiornaChipDisponibili()
            _firmaSalvata = CalcolaFirma()
        End Sub

        ''' <summary>Modalità in memoria: usata dentro la maschera principale, prima che l'interruttore
        ''' sia stato salvato.</summary>
        Public Sub New(repository As InterruttoreRepository, nomeInterruttore As String,
                       taglieDisponibili As IEnumerable(Of String),
                       lineeProdottoDisponibili As IEnumerable(Of LineaProdotto),
                       celleCorrenti As IEnumerable(Of ConfigurazioneCella))
            _repository = repository
            _idInterruttore = Nothing

            Inizializza()

            NomeInterruttore = If(String.IsNullOrWhiteSpace(nomeInterruttore), "Nuovo interruttore", nomeInterruttore)
            _tutteLeTaglie = taglieDisponibili.ToList()
            _tutteLeLineeProdotto = lineeProdottoDisponibili.ToList()

            CaricaGruppiIniziali(celleCorrenti.ToList(), _tutteLeLineeProdotto)
            AggiornaChipDisponibili()
            _firmaSalvata = CalcolaFirma()
        End Sub

        Private Sub Inizializza()
            TaglieDisponibili = New ObservableCollection(Of TagliaSelezionabile)
            LineeProdottoDisponibili = New ObservableCollection(Of LineaProdottoSelezionabile)
            Gruppi = New ObservableCollection(Of GruppoCella)

            AggiungiGruppoCommand = New RelayCommand(AddressOf EseguiAggiungiGruppo, AddressOf PuoAggiungereGruppo)
            SalvaCommand = New RelayCommand(AddressOf EseguiSalva, Function() Gruppi.Count > 0)
            SelezionaTutteTaglieCommand = New RelayCommand(AddressOf EseguiSelezionaTutteTaglie, Function() TaglieDisponibili.Count > 0)

            _timerConferma = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2.5)}
            AddHandler _timerConferma.Tick, Sub()
                                                 _timerConferma.Stop()
                                                 MostraConferma = False
                                             End Sub
        End Sub

        ''' <summary>
        ''' Ricostruisce le chip selezionabili. Le taglie già assegnate alla linea prodotto
        ''' correntemente selezionata vengono escluse (una stessa taglia non può ripetersi due
        ''' volte per la stessa linea); le linee prodotto restano sempre tutte selezionabili,
        ''' perché una linea può avere più assegnazioni, una per ciascun sottoinsieme di taglie.
        ''' </summary>
        Private Sub AggiornaChipDisponibili()
            Dim idLineaSelezionata = LineeProdottoDisponibili.FirstOrDefault(Function(l) l.Selezionata)?.Id

            Dim taglieUsate As HashSet(Of String)
            If idLineaSelezionata.HasValue Then
                taglieUsate = Gruppi.
                    Where(Function(g) g.LineaProdotto.Id = idLineaSelezionata.Value).
                    SelectMany(Function(g) g.Taglie).ToHashSet()
            Else
                taglieUsate = New HashSet(Of String)()
            End If

            Dim taglieSelezionatePrecedenti = TaglieDisponibili.Where(Function(t) t.Selezionata).Select(Function(t) t.Taglia).ToHashSet()

            TaglieDisponibili.Clear()
            For Each taglia In _tutteLeTaglie.Where(Function(t) Not taglieUsate.Contains(t))
                Dim riga As New TagliaSelezionabile With {.Taglia = taglia, .Selezionata = taglieSelezionatePrecedenti.Contains(taglia)}
                AddHandler riga.PropertyChanged, Sub() DirectCast(AggiungiGruppoCommand, RelayCommand).RaiseCanExecuteChanged()
                TaglieDisponibili.Add(riga)
            Next

            If LineeProdottoDisponibili.Count = 0 Then
                For Each linea In _tutteLeLineeProdotto
                    Dim riga As New LineaProdottoSelezionabile(linea)
                    AddHandler riga.PropertyChanged, AddressOf LineaProdotto_PropertyChanged
                    LineeProdottoDisponibili.Add(riga)
                Next
            End If

            DirectCast(AggiungiGruppoCommand, RelayCommand).RaiseCanExecuteChanged()
            DirectCast(SelezionaTutteTaglieCommand, RelayCommand).RaiseCanExecuteChanged()
        End Sub

        ''' <summary>Selezione singola: quando una linea prodotto viene selezionata, le altre si deselezionano;
        ''' cambia anche l'insieme di taglie già usate da escludere, quindi si ricostruiscono le chip.</summary>
        Private Sub LineaProdotto_PropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            If e.PropertyName <> NameOf(LineaProdottoSelezionabile.Selezionata) Then Return

            Dim riga = DirectCast(sender, LineaProdottoSelezionabile)
            If riga.Selezionata Then
                For Each altra In LineeProdottoDisponibili.Where(Function(l) Not ReferenceEquals(l, riga) AndAlso l.Selezionata)
                    altra.Selezionata = False
                Next
            End If

            AggiornaChipDisponibili()
            DirectCast(AggiungiGruppoCommand, RelayCommand).RaiseCanExecuteChanged()
        End Sub

        ''' <summary>
        ''' Ricostruisce i gruppi visualizzati raggruppando le celle esistenti per linea prodotto e
        ''' valori dimensionali identici: celle con la stessa linea e gli stessi 5 valori vengono
        ''' mostrate come un unico gruppo (una linea, più taglie), invece che come righe singole.
        ''' </summary>
        Private Sub CaricaGruppiIniziali(celle As List(Of ConfigurazioneCella), lineeProdotto As List(Of LineaProdotto))
            Gruppi.Clear()
            Dim raggruppate = celle.GroupBy(Function(c) (c.IdLineaProdotto, c.AltezzaCella, c.LarghezzaCella, c.AltezzaCellaVerticale, c.LarghezzaCellaVerticale, c.ProfonditaCella))
            For Each gruppo In raggruppate
                Dim taglieGruppo = gruppo.Select(Function(c) c.Taglia).Distinct().ToList()
                Dim linea = lineeProdotto.FirstOrDefault(Function(l) l.Id = gruppo.Key.IdLineaProdotto)
                If linea Is Nothing Then Continue For

                Gruppi.Add(New GruppoCella With {
                    .LineaProdotto = linea,
                    .Taglie = taglieGruppo,
                    .AltezzaCella = gruppo.Key.AltezzaCella,
                    .LarghezzaCella = gruppo.Key.LarghezzaCella,
                    .AltezzaCellaVerticale = gruppo.Key.AltezzaCellaVerticale,
                    .LarghezzaCellaVerticale = gruppo.Key.LarghezzaCellaVerticale,
                    .ProfonditaCella = gruppo.Key.ProfonditaCella
                })
            Next
        End Sub

        Public Property TaglieDisponibili As ObservableCollection(Of TagliaSelezionabile)
        Public Property LineeProdottoDisponibili As ObservableCollection(Of LineaProdottoSelezionabile)
        Public Property Gruppi As ObservableCollection(Of GruppoCella)

        Public Property NomeInterruttore As String

        Public Property NuovaAltezzaCella As Double?
        Public Property NuovaLarghezzaCella As Double?
        Public Property NuovaAltezzaCellaVerticale As Double?
        Public Property NuovaLarghezzaCellaVerticale As Double?
        Public Property NuovaProfonditaCella As Double?

        Public Property AggiungiGruppoCommand As RelayCommand
        Public Property SelezionaTutteTaglieCommand As RelayCommand
        Public Property SalvaCommand As RelayCommand

        ''' <summary>Valorizzato dopo il salvataggio in modalità in memoria (Nothing in modalità DB).</summary>
        Public Property CelleModificate As List(Of ConfigurazioneCella)

        ''' <summary>Sollevato quando, in modalità in memoria, il salvataggio è completato: la finestra può chiudersi.</summary>
        Public Event Confermato As EventHandler

        Private _mostraConferma As Boolean
        Public Property MostraConferma As Boolean
            Get
                Return _mostraConferma
            End Get
            Set(value As Boolean)
                _mostraConferma = value
                OnPropertyChanged()
            End Set
        End Property

        Private Function PuoAggiungereGruppo() As Boolean
            Return TaglieDisponibili.Any(Function(t) t.Selezionata) AndAlso LineeProdottoDisponibili.Any(Function(l) l.Selezionata)
        End Function

        ''' <summary>Se sono già tutte selezionate le deseleziona, altrimenti le seleziona tutte.</summary>
        Private Sub EseguiSelezionaTutteTaglie()
            Dim selezionaTutte = Not TaglieDisponibili.All(Function(t) t.Selezionata)
            For Each taglia In TaglieDisponibili
                taglia.Selezionata = selezionaTutte
            Next
        End Sub

        Private Sub EseguiAggiungiGruppo()
            Dim taglieScelte = TaglieDisponibili.Where(Function(t) t.Selezionata).Select(Function(t) t.Taglia).ToList()
            Dim lineaScelta = LineeProdottoDisponibili.Where(Function(l) l.Selezionata).Select(Function(l) New LineaProdotto With {.Id = l.Id, .Nome = l.Nome}).FirstOrDefault()
            If taglieScelte.Count = 0 OrElse lineaScelta Is Nothing Then Return

            Gruppi.Add(New GruppoCella With {
                .LineaProdotto = lineaScelta,
                .Taglie = taglieScelte,
                .AltezzaCella = NuovaAltezzaCella,
                .LarghezzaCella = NuovaLarghezzaCella,
                .AltezzaCellaVerticale = NuovaAltezzaCellaVerticale,
                .LarghezzaCellaVerticale = NuovaLarghezzaCellaVerticale,
                .ProfonditaCella = NuovaProfonditaCella
            })

            NuovaAltezzaCella = Nothing
            NuovaLarghezzaCella = Nothing
            NuovaAltezzaCellaVerticale = Nothing
            NuovaLarghezzaCellaVerticale = Nothing
            NuovaProfonditaCella = Nothing
            OnPropertyChanged(NameOf(NuovaAltezzaCella))
            OnPropertyChanged(NameOf(NuovaLarghezzaCella))
            OnPropertyChanged(NameOf(NuovaAltezzaCellaVerticale))
            OnPropertyChanged(NameOf(NuovaLarghezzaCellaVerticale))
            OnPropertyChanged(NameOf(NuovaProfonditaCella))

            AggiornaChipDisponibili() ' toglie dalla selezione le taglie/linee appena assegnate
            DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
        End Sub

        Public Sub RimuoviGruppo(gruppo As GruppoCella)
            If gruppo Is Nothing Then Return
            Gruppi.Remove(gruppo)
            AggiornaChipDisponibili() ' le taglie/linee di quel gruppo tornano selezionabili
            DirectCast(SalvaCommand, RelayCommand).RaiseCanExecuteChanged()
        End Sub

        Private Function CalcolaFirma() As HashSet(Of String)
            Return New HashSet(Of String)(
                Gruppi.Select(Function(g)
                                  Dim taglie = String.Join(",", g.Taglie.OrderBy(Function(t) t))
                                  Return $"{taglie}|{g.LineaProdotto.Id}|{g.AltezzaCella}|{g.LarghezzaCella}|{g.AltezzaCellaVerticale}|{g.LarghezzaCellaVerticale}|{g.ProfonditaCella}"
                              End Function))
        End Function

        ''' <summary>Usato dalla finestra per capire se avvisare alla chiusura.</summary>
        Public Function CiSonoModificheNonSalvate() As Boolean
            Return Not CalcolaFirma().SetEquals(_firmaSalvata)
        End Function

        Private Sub EseguiSalva()
            Dim celle As New List(Of ConfigurazioneCella)
            For Each gruppo In Gruppi
                For Each taglia In gruppo.Taglie
                    celle.Add(New ConfigurazioneCella With {
                        .IdLineaProdotto = gruppo.LineaProdotto.Id,
                        .IdInterruttore = If(_idInterruttore, 0),
                        .Taglia = taglia,
                        .AltezzaCella = gruppo.AltezzaCella,
                        .LarghezzaCella = gruppo.LarghezzaCella,
                        .AltezzaCellaVerticale = gruppo.AltezzaCellaVerticale,
                        .LarghezzaCellaVerticale = gruppo.LarghezzaCellaVerticale,
                        .ProfonditaCella = gruppo.ProfonditaCella
                    })
                Next
            Next

            If _idInterruttore.HasValue Then
                _repository.SalvaCelle(_idInterruttore.Value, celle)
                _firmaSalvata = CalcolaFirma()
                _timerConferma.Stop()
                MostraConferma = True
                _timerConferma.Start()
            Else
                CelleModificate = celle
                _firmaSalvata = CalcolaFirma()
                RaiseEvent Confermato(Me, EventArgs.Empty)
            End If
        End Sub

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(<CallerMemberName> Optional nomeProprieta As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(nomeProprieta))
        End Sub

    End Class
End Namespace
