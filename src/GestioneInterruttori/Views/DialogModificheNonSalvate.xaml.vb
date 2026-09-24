Namespace Views
    ''' <summary>
    ''' Dialog personalizzato (sostituisce MessageBox.Show) per chiedere se salvare le modifiche
    ''' in sospeso prima di cambiare pagina o chiudere una maschera.
    ''' </summary>
    Public Class DialogModificheNonSalvate

        Public Property Risultato As MessageBoxResult = MessageBoxResult.Cancel

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>Mostra il dialog e restituisce Yes (salva), No (scarta) o Cancel (annulla).</summary>
        Public Shared Function Chiedi(owner As Window, Optional messaggio As String = Nothing) As MessageBoxResult
            Dim dialog As New DialogModificheNonSalvate()
            If owner IsNot Nothing Then
                dialog.Owner = owner
            Else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen
            End If
            If Not String.IsNullOrWhiteSpace(messaggio) Then
                dialog.TestoMessaggio.Text = messaggio
            End If
            dialog.ShowDialog()
            Return dialog.Risultato
        End Function

        Private Sub Salva_Click(sender As Object, e As RoutedEventArgs)
            Risultato = MessageBoxResult.Yes
            Close()
        End Sub

        Private Sub NonSalvare_Click(sender As Object, e As RoutedEventArgs)
            Risultato = MessageBoxResult.No
            Close()
        End Sub

        Private Sub Annulla_Click(sender As Object, e As RoutedEventArgs)
            Risultato = MessageBoxResult.Cancel
            Close()
        End Sub

    End Class
End Namespace
