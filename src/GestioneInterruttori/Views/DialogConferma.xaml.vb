Namespace Views
    ''' <summary>
    ''' Dialog personalizzato (sostituisce MessageBox.Show) per chiedere conferma prima di
    ''' un'operazione distruttiva (es. rimozione di una taglia, linea prodotto o assegnazione).
    ''' </summary>
    Public Class DialogConferma

        Public Property Confermato As Boolean

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>Mostra il dialog e restituisce True se l'utente ha confermato.</summary>
        Public Shared Function Chiedi(owner As Window, messaggio As String,
                                      Optional titolo As String = "Conferma rimozione",
                                      Optional testoPulsanteConferma As String = "Rimuovi") As Boolean
            Dim dialog As New DialogConferma()
            If owner IsNot Nothing Then
                dialog.Owner = owner
            Else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen
            End If
            dialog.TestoTitolo.Text = titolo
            dialog.TestoMessaggio.Text = messaggio
            dialog.PulsanteConfermaBtn.Content = testoPulsanteConferma
            dialog.ShowDialog()
            Return dialog.Confermato
        End Function

        Private Sub Conferma_Click(sender As Object, e As RoutedEventArgs)
            Confermato = True
            Close()
        End Sub

        Private Sub Annulla_Click(sender As Object, e As RoutedEventArgs)
            Confermato = False
            Close()
        End Sub

    End Class
End Namespace
