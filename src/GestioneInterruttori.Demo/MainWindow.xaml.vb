Imports GestioneInterruttori.Views

Class MainWindow

    Private ReadOnly _percorsoDatabase As String = "C:\applicazioni\DKC\Interruttori.sqlite"

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub ApriGestioneAnagrafica_Click(sender As Object, e As RoutedEventArgs)
        Dim finestra As New GestioneInterruttoreWindow(_percorsoDatabase)
        finestra.Owner = Me
        finestra.ShowDialog()
    End Sub

    Private Sub ApriConfigurazioni_Click(sender As Object, e As RoutedEventArgs)
        Dim finestra As New ConfigurazioneInterruttoreWindow(_percorsoDatabase, idInterruttore:=1)
        finestra.Owner = Me
        finestra.ShowDialog()
    End Sub

End Class
