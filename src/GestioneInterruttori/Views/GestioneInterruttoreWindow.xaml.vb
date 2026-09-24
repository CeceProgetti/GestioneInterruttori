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

        ''' <summary>
        ''' Apre la maschera Configurazione interruttore in modalità "in memoria": può essere
        ''' compilata prima ancora che l'interruttore sia stato salvato. Il risultato viene
        ''' riportato nel ViewModel solo se l'utente conferma (DialogResult = True).
        ''' </summary>
        Private Sub ApriConfigurazioneInterruttore(sender As Object, e As EventArgs)
            Dim finestra As New ConfigurazioneInterruttoreWindow(_percorsoDatabase, _viewModel.Nome, _viewModel.ConfigurazioniPendenti)
            finestra.Owner = Me
            If finestra.ShowDialog() = True Then
                _viewModel.AggiornaConfigurazioniPendenti(finestra.ConfigurazioniModificate)
            End If
        End Sub

        Private Sub ApriConfigurazioneCella(sender As Object, e As EventArgs)
            MessageBox.Show("La maschera Configurazione cella non è ancora stata realizzata.",
                             "In sviluppo", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

    End Class
End Namespace
