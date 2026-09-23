Imports GestioneInterruttori.Data
Imports GestioneInterruttori.ViewModels

Namespace Views
    ''' <summary>
    ''' Maschera di gestione anagrafica interruttori (inserimento/modifica/eliminazione),
    ''' riservata agli utenti UT DKC.
    ''' </summary>
    Public Class GestioneInterruttoreWindow

        Private ReadOnly _percorsoDatabase As String
        Private ReadOnly _viewModel As GestioneInterruttoreViewModel

        ''' <param name="percorsoDatabase">Percorso del file Interruttori.sqlite, fornito dall'applicazione ospitante.</param>
        Public Sub New(percorsoDatabase As String)
            InitializeComponent()

            _percorsoDatabase = percorsoDatabase
            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New GestioneInterruttoreViewModel(repository)

            AddHandler _viewModel.RichiestaAperturaConfigurazioneInterruttore, AddressOf ApriConfigurazioneInterruttore
            AddHandler _viewModel.RichiestaAperturaConfigurazioneCella, AddressOf ApriConfigurazioneCella

            DataContext = _viewModel
        End Sub

        Private Sub ApriConfigurazioneInterruttore(sender As Object, e As EventArgs)
            Dim finestra As New ConfigurazioneInterruttoreWindow(_percorsoDatabase, _viewModel.InterruttoreSelezionato.Id)
            finestra.Owner = Me
            finestra.ShowDialog()
            _viewModel.RicaricaConfigurazioniAssegnate()
        End Sub

        Private Sub ApriConfigurazioneCella(sender As Object, e As EventArgs)
            MessageBox.Show("La maschera Configurazione cella non è ancora stata realizzata.",
                             "In sviluppo", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

    End Class
End Namespace
