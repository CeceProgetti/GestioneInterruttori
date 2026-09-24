Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows.Threading
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' ViewModel della sola sezione "Configurazioni".
    ''' Due modalità:
    ''' - DB (idInterruttore valido): legge/scrive direttamente su ConfigurazioneInterruttore.
    '''   Usata per testare/lavorare su questa sola parte, fuori dal flusso della maschera intera.
    ''' - In memoria (nessun idInterruttore): lavora su una lista passata dal chiamante e la
    '''   restituisce alla chiusura, senza toccare il DB. Usata dentro la maschera principale,
    '''   dove Configurazione interruttore può essere compilata prima che l'interruttore stesso
    '''   sia stato salvato (il salvataggio vero avviene tutto insieme, alla fine).
    ''' </summary>
    Public Class ConfigurazioneInterruttoreViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _repository As InterruttoreRepository
        Private ReadOnly _idInterruttore As Integer?
        Private ReadOnly _timerConferma As DispatcherTimer

        ''' <summary>Istantanea (Id|DeltaH|DeltaL|DeltaP delle opzioni abilitate) presa all'apertura e
        ''' dopo ogni salvataggio, per capire se ci sono modifiche non salvate alla chiusura.</summary>
        Private _firmaSalvata As HashSet(Of String)

        ''' <summary>Modalità DB: legge/scrive subito sull'interruttore già salvato.</summary>
        Public Sub New(repository As InterruttoreRepository, idInterruttore As Integer)
            _repository = repository
            _idInterruttore = idInterruttore

            Configurazioni = New ObservableCollection(Of ConfigurazioneAssegnabile)
            GruppiConfigurazione = New ObservableCollection(Of GruppoConfigurazione)
            SalvaCommand = New RelayCommand(AddressOf EseguiSalva)

            _timerConferma = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2.5)}
            AddHandler _timerConferma.Tick, Sub()
                                                 _timerConferma.Stop()
                                                 MostraConferma = False
                                             End Sub

            Dim interruttore = _repository.OttieniInterruttore(idInterruttore)
            NomeInterruttore = If(interruttore?.Nome, "—")
            CaricaCatalogo(_repository.ConfigurazioniDiInterruttore(idInterruttore))
            _firmaSalvata = CalcolaFirma()
        End Sub

        ''' <summary>Modalità in memoria: parte dalle configurazioni già scelte (eventualmente nessuna),
        ''' e alla chiusura le restituisce tramite <see cref="ConfigurazioniModificate"/>.</summary>
        Public Sub New(repository As InterruttoreRepository, nomeInterruttore As String, configurazioniCorrenti As IEnumerable(Of ConfigurazioneInterruttore))
            _repository = repository
            _idInterruttore = Nothing

            Configurazioni = New ObservableCollection(Of ConfigurazioneAssegnabile)
            GruppiConfigurazione = New ObservableCollection(Of GruppoConfigurazione)
            SalvaCommand = New RelayCommand(AddressOf EseguiSalva)

            _timerConferma = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2.5)}
            AddHandler _timerConferma.Tick, Sub()
                                                 _timerConferma.Stop()
                                                 MostraConferma = False
                                             End Sub

            NomeInterruttore = If(String.IsNullOrWhiteSpace(nomeInterruttore), "Nuovo interruttore", nomeInterruttore)
            CaricaCatalogo(configurazioniCorrenti.ToList())
            _firmaSalvata = CalcolaFirma()
        End Sub

        Public ReadOnly Property Configurazioni As ObservableCollection(Of ConfigurazioneAssegnabile)
        Public ReadOnly Property GruppiConfigurazione As ObservableCollection(Of GruppoConfigurazione)
        Public ReadOnly Property SalvaCommand As RelayCommand

        Public Property NomeInterruttore As String

        ''' <summary>Valorizzato dopo il salvataggio in modalità in memoria (Nothing in modalità DB).</summary>
        Public Property ConfigurazioniModificate As List(Of ConfigurazioneInterruttore)

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

        Private Sub CaricaCatalogo(assegnate As List(Of ConfigurazioneInterruttore))
            Configurazioni.Clear()
            For Each config In _repository.ElencoConfigurazioniCatalogo()
                Dim riga As New ConfigurazioneAssegnabile(config)
                Dim assegnazione = assegnate.FirstOrDefault(Function(a) a.IdConfigurazione = config.Id)
                If assegnazione IsNot Nothing Then
                    riga.Abilitata = True
                    riga.DeltaH = assegnazione.DeltaH
                    riga.DeltaL = assegnazione.DeltaL
                    riga.DeltaP = assegnazione.DeltaP
                End If
                Configurazioni.Add(riga)
            Next

            GruppiConfigurazione.Clear()
            For Each gruppo In Configurazioni.GroupBy(Function(c) c.Tipo)
                GruppiConfigurazione.Add(New GruppoConfigurazione With {
                    .Tipo = gruppo.Key,
                    .Opzioni = New ObservableCollection(Of ConfigurazioneAssegnabile)(gruppo)
                })
            Next
        End Sub

        Private Function CalcolaFirma() As HashSet(Of String)
            Return New HashSet(Of String)(
                Configurazioni.Where(Function(c) c.Abilitata).
                Select(Function(c) $"{c.IdConfigurazione}|{c.DeltaH}|{c.DeltaL}|{c.DeltaP}"))
        End Function

        ''' <summary>Usato dalla finestra per capire se avvisare alla chiusura.</summary>
        Public Function CiSonoModificheNonSalvate() As Boolean
            Return Not CalcolaFirma().SetEquals(_firmaSalvata)
        End Function

        Private Sub EseguiSalva()
            Dim configurazioniAbilitate = Configurazioni.
                Where(Function(c) c.Abilitata).
                Select(Function(c) New ConfigurazioneInterruttore With {
                    .IdInterruttore = If(_idInterruttore, 0),
                    .IdConfigurazione = c.IdConfigurazione,
                    .DeltaH = c.DeltaH,
                    .DeltaL = c.DeltaL,
                    .DeltaP = c.DeltaP
                }).ToList()

            If _idInterruttore.HasValue Then
                _repository.SalvaConfigurazioni(_idInterruttore.Value, configurazioniAbilitate)
                _firmaSalvata = CalcolaFirma()
                _timerConferma.Stop()
                MostraConferma = True
                _timerConferma.Start()
            Else
                ConfigurazioniModificate = configurazioniAbilitate
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
