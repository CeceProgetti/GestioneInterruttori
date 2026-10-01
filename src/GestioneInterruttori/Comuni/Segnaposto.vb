Namespace Comuni

    ''' <summary>
    ''' Testo segnaposto (placeholder) per le TextBox: <c>comuni:Segnaposto.Testo="Cerca…"</c>.
    ''' Viene disegnato dal template condiviso delle TextBox (Stili\CampiInput.xaml) nella stessa
    ''' posizione del testo digitato, così il cursore resta allineato all'inizio della scritta, e sparisce
    ''' appena il campo contiene qualcosa.
    ''' </summary>
    Public Class Segnaposto

        Public Shared ReadOnly TestoProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("Testo", GetType(String), GetType(Segnaposto), New PropertyMetadata(Nothing))

        Public Shared Function GetTesto(elemento As DependencyObject) As String
            Return CStr(elemento.GetValue(TestoProperty))
        End Function

        Public Shared Sub SetTesto(elemento As DependencyObject, valore As String)
            elemento.SetValue(TestoProperty, valore)
        End Sub

    End Class

End Namespace
