Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Windows.Threading
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' ViewModel della sola sezione "Configurazioni" per un interruttore specifico
    ''' (finestra isolata, utile per svilupparla/testarla senza passare dalla maschera intera).
    ''' </summary>
    Public Class ConfigurazioneInterruttoreViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _repository As InterruttoreRepository
        Private ReadOnly _idInterruttore As Integer
        Private ReadOnly _timerConferma As DispatcherTimer

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

            Carica()
        End Sub

        Public ReadOnly Property Configurazioni As ObservableCollection(Of ConfigurazioneAssegnabile)
        Public ReadOnly Property GruppiConfigurazione As ObservableCollection(Of GruppoConfigurazione)
        Public ReadOnly Property SalvaCommand As RelayCommand

        Public Property NomeInterruttore As String

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

        Private Sub Carica()
            Dim interruttore = _repository.OttieniInterruttore(_idInterruttore)
            NomeInterruttore = If(interruttore?.Nome, "—")

            Dim assegnate = _repository.ConfigurazioniDiInterruttore(_idInterruttore)

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

        Private Sub EseguiSalva()
            Dim configurazioniAbilitate = Configurazioni.
                Where(Function(c) c.Abilitata).
                Select(Function(c) New ConfigurazioneInterruttore With {
                    .IdInterruttore = _idInterruttore,
                    .IdConfigurazione = c.IdConfigurazione,
                    .DeltaH = c.DeltaH,
                    .DeltaL = c.DeltaL,
                    .DeltaP = c.DeltaP
                })

            _repository.SalvaConfigurazioni(_idInterruttore, configurazioniAbilitate)

            _timerConferma.Stop()
            MostraConferma = True
            _timerConferma.Start()
        End Sub

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(<CallerMemberName> Optional nomeProprieta As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(nomeProprieta))
        End Sub

    End Class
End Namespace
