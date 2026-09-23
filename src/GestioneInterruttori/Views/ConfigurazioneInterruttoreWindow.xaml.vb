Imports GestioneInterruttori.Data
Imports GestioneInterruttori.ViewModels

Namespace Views
    ''' <summary>Finestra isolata sulla sola sezione Configurazioni di un interruttore.</summary>
    Public Class ConfigurazioneInterruttoreWindow

        Public Sub New(percorsoDatabase As String, idInterruttore As Integer)
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            DataContext = New ConfigurazioneInterruttoreViewModel(repository, idInterruttore)
        End Sub

    End Class
End Namespace
