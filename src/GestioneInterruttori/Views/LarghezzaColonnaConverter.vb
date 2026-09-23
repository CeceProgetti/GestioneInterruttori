Imports System.Globalization
Imports System.Windows.Data

Namespace Views
    ''' <summary>
    ''' Calcola la larghezza di una card "Tipo" in modo che ce ne stiano al massimo 3 per riga,
    ''' indipendentemente dalla larghezza dello schermo (oltre le 3 vanno a capo).
    ''' </summary>
    Public Class LarghezzaColonnaConverter
        Implements IValueConverter

        Private Const NumeroColonne As Integer = 3
        Private Const MargineDestro As Double = 16
        Private Const LarghezzaMinima As Double = 280

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim larghezzaDisponibile As Double = 0
            If TypeOf value Is Double Then
                larghezzaDisponibile = CDbl(value)
            End If

            If larghezzaDisponibile <= 0 Then
                Return LarghezzaMinima
            End If

            Dim larghezzaCard = (larghezzaDisponibile - MargineDestro * NumeroColonne) / NumeroColonne
            Return Math.Max(larghezzaCard, LarghezzaMinima)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException()
        End Function
    End Class
End Namespace
