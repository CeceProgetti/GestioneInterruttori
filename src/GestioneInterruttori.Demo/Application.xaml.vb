Imports GestioneInterruttori.Views

Class Application

    ''' <summary>
    ''' Progetto "vetrina" per avviare e debuggare le maschere della libreria fuori dal contesto
    ''' dell'app che le integrerà. Non fa parte dell'integrazione finale.
    ''' </summary>

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        Dim finestra As New GestioneInterruttoreWindow("C:\applicazioni\DKC\Interruttori.sqlite")
        MainWindow = finestra
        finestra.Show()
    End Sub

End Class
