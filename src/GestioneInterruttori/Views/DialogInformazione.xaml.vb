Imports System.Windows.Media

Namespace Views

    ''' <summary>
    ''' Dialog informativo con un solo pulsante OK (sostituisce MessageBox.Show per i messaggi
    ''' che non chiedono una scelta), nello stesso stile di DialogConferma.
    ''' </summary>
    Public Class DialogInformazione

        Public Enum Tipo
            Successo
            Avviso
            Informazione
        End Enum

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <param name="evidenziato">Testo mostrato in grassetto sotto il messaggio (es. il valore scelto); facoltativo.</param>
        Public Shared Sub Mostra(owner As Window, titolo As String, messaggio As String,
                                 Optional evidenziato As String = Nothing, Optional tipo As Tipo = Tipo.Informazione)
            Dim dialog As New DialogInformazione()
            If owner IsNot Nothing Then
                dialog.Owner = owner
            Else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen
            End If
            dialog.Title = titolo
            dialog.TestoTitolo.Text = titolo
            dialog.TestoMessaggio.Text = messaggio
            dialog.TestoEvidenziato.Text = If(evidenziato, "")
            dialog.TestoEvidenziato.Visibility = If(String.IsNullOrEmpty(evidenziato), Visibility.Collapsed, Visibility.Visible)
            dialog.ImpostaIcona(tipo)
            dialog.ShowDialog()
        End Sub

        Private Sub ImpostaIcona(tipo As Tipo)
            Select Case tipo
                Case Tipo.Successo
                    TestoIcona.Text = ChrW(&H2713) ' ✓
                    CerchioIcona.Background = New SolidColorBrush(Color.FromRgb(&HE3, &HF4, &HEA))
                    TestoIcona.Foreground = New SolidColorBrush(Color.FromRgb(&H1E, &H8E, &H4E))
                Case Tipo.Avviso
                    TestoIcona.Text = "!"
                    CerchioIcona.Background = New SolidColorBrush(Color.FromRgb(&HFB, &HE7, &HE9))
                    TestoIcona.Foreground = New SolidColorBrush(Color.FromRgb(&HD8, &H30, &H3F))
                Case Else
                    TestoIcona.Text = "i"
                    CerchioIcona.Background = New SolidColorBrush(Color.FromRgb(&HEE, &HF0, &HF4))
                    TestoIcona.Foreground = New SolidColorBrush(Color.FromRgb(&H6B, &H72, &H80))
            End Select
        End Sub

        Private Sub Ok_Click(sender As Object, e As RoutedEventArgs)
            Close()
        End Sub

    End Class

End Namespace
