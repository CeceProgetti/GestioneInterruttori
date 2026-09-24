Imports System.Globalization
Imports System.Windows.Data

Namespace Views
    ''' <summary>
    ''' Converte tra Double? e il testo di una TextBox: una stringa vuota (campo svuotato
    ''' dall'utente) diventa Nothing invece di far fallire il binding con una FormatException.
    ''' </summary>
    Public Class NumeroNullableConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing Then Return String.Empty
            Return DirectCast(value, Double?).Value.ToString(culture)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Dim testo = TryCast(value, String)
            If String.IsNullOrWhiteSpace(testo) Then Return Nothing

            Dim numero As Double
            If Double.TryParse(testo, NumberStyles.Float, culture, numero) Then
                Return numero
            End If

            Return Binding.DoNothing
        End Function
    End Class
End Namespace
