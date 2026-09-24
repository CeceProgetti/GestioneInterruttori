Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models
Imports GestioneInterruttori.ViewModels

Namespace Views
    ''' <summary>Finestra isolata sulla sola sezione Configurazioni di un interruttore.</summary>
    Public Class ConfigurazioneInterruttoreWindow

        Private ReadOnly _viewModel As ConfigurazioneInterruttoreViewModel

        ''' <summary>Modalità DB: interruttore già salvato, legge/scrive direttamente su ConfigurazioneInterruttore.</summary>
        Public Sub New(percorsoDatabase As String, idInterruttore As Integer)
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New ConfigurazioneInterruttoreViewModel(repository, idInterruttore)
            DataContext = _viewModel
        End Sub

        ''' <summary>Modalità in memoria: usata dentro la maschera principale, prima che l'interruttore
        ''' sia stato salvato. Alla conferma, chiude la finestra e i dati si leggono da <see cref="ConfigurazioniModificate"/>.</summary>
        Public Sub New(percorsoDatabase As String, nomeInterruttore As String, configurazioniCorrenti As IEnumerable(Of ConfigurazioneInterruttore))
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New ConfigurazioneInterruttoreViewModel(repository, nomeInterruttore, configurazioniCorrenti)
            AddHandler _viewModel.Confermato, Sub()
                                                   DialogResult = True
                                                   Close()
                                               End Sub
            DataContext = _viewModel
        End Sub

        ''' <summary>Valorizzato solo se aperta in modalità in memoria e confermata (DialogResult = True).</summary>
        Public ReadOnly Property ConfigurazioniModificate As List(Of ConfigurazioneInterruttore)
            Get
                Return _viewModel.ConfigurazioniModificate
            End Get
        End Property

    End Class
End Namespace
