Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models
Imports GestioneInterruttori.ViewModels

Namespace Views
    Public Class SelezionaInterruttoreWindow

        Private ReadOnly _viewModel As SelezionaInterruttoreViewModel

        ''' <summary>
        ''' Apre la maschera di selezione interruttore.
        ''' </summary>
        ''' <param name="percorsoDatabase">Percorso del file Interruttori.sqlite, fornito dall'applicazione ospitante.</param>
        Public Sub New(percorsoDatabase As String)
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New SelezionaInterruttoreViewModel(repository)
            AddHandler _viewModel.RichiestaChiusura, AddressOf OnRichiestaChiusura

            DataContext = _viewModel
        End Sub

        Private Sub OnRichiestaChiusura(sender As Object, e As EventArgs)
            DialogResult = True
            Close()
        End Sub

        ''' <summary>L'interruttore scelto dall'utente, valorizzato solo se la maschera è stata confermata.</summary>
        Public ReadOnly Property InterruttoreSelezionato As Interruttore
            Get
                Return _viewModel.InterruttoreSelezionato
            End Get
        End Property

    End Class
End Namespace
