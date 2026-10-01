Imports System.Windows.Controls.Primitives
Imports System.Windows.Input
Imports System.Windows.Media

Namespace Comuni

    ''' <summary>
    ''' Standard per tutte le finestre: cliccando su una zona "vuota" (sfondo, card, etichette...) la
    ''' casella di testo attiva perde il focus, come ci si aspetta. WPF di suo lascia il focus sul campo.
    ''' Uso sulla finestra: <c>comuni:DeselezioneCampi.Attiva="True"</c>.
    ''' Se il clic cade su un controllo che prende il focus (pulsante, altra casella, elenco...) non fa
    ''' nulla: il focus passa a quel controllo normalmente.
    ''' </summary>
    Public Class DeselezioneCampi

        Public Shared ReadOnly AttivaProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("Attiva", GetType(Boolean), GetType(DeselezioneCampi),
                                                New PropertyMetadata(False, AddressOf AttivaCambiata))

        Public Shared Function GetAttiva(elemento As DependencyObject) As Boolean
            Return CBool(elemento.GetValue(AttivaProperty))
        End Function

        Public Shared Sub SetAttiva(elemento As DependencyObject, valore As Boolean)
            elemento.SetValue(AttivaProperty, valore)
        End Sub

        Private Shared Sub AttivaCambiata(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim finestra = TryCast(d, Window)
            If finestra Is Nothing Then Return
            RemoveHandler finestra.PreviewMouseDown, AddressOf Finestra_PreviewMouseDown
            If CBool(e.NewValue) Then AddHandler finestra.PreviewMouseDown, AddressOf Finestra_PreviewMouseDown
        End Sub

        Private Shared Sub Finestra_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim finestra = DirectCast(sender, Window)
            If Not (TypeOf Keyboard.FocusedElement Is TextBoxBase) Then Return
            If CadeSuControlloFocalizzabile(TryCast(e.OriginalSource, DependencyObject)) Then Return

            ' Tolgo il focus dal campo (i binding "LostFocus" si aggiornano come quando si esce col Tab)
            ' e lo lascio alla finestra, così i tasti rapidi (Esc, Invio) continuano a funzionare.
            Dim campo = DirectCast(Keyboard.FocusedElement, DependencyObject)
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(campo), Nothing)
            Keyboard.ClearFocus()
            finestra.Focus()
        End Sub

        ''' <summary>True se l'elemento cliccato è (o sta dentro) un controllo che può ricevere il focus.</summary>
        Private Shared Function CadeSuControlloFocalizzabile(elemento As DependencyObject) As Boolean
            While elemento IsNot Nothing AndAlso Not TypeOf elemento Is Window
                Dim controllo = TryCast(elemento, Control)
                ' Gli ScrollViewer contengono intere pagine: un clic sul loro sfondo conta come zona vuota.
                If controllo IsNot Nothing AndAlso Not TypeOf controllo Is ScrollViewer AndAlso
                   controllo.Focusable AndAlso controllo.IsEnabled Then Return True
                elemento = Genitore(elemento)
            End While
            Return False
        End Function

        Private Shared Function Genitore(elemento As DependencyObject) As DependencyObject
            If TypeOf elemento Is Visual OrElse TypeOf elemento Is Media3D.Visual3D Then
                Dim g = VisualTreeHelper.GetParent(elemento)
                If g IsNot Nothing Then Return g
            End If
            Return LogicalTreeHelper.GetParent(elemento) ' es. Run dentro un TextBlock
        End Function

    End Class

End Namespace
