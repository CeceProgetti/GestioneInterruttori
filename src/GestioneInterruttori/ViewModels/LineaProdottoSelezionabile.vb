Imports System.ComponentModel
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>Una linea prodotto (tra quelle già scelte nella maschera principale), selezionabile a chip.</summary>
    Public Class LineaProdottoSelezionabile
        Implements INotifyPropertyChanged

        Public Sub New(lineaProdotto As LineaProdotto)
            Id = lineaProdotto.Id
            Nome = lineaProdotto.Nome
        End Sub

        Public Property Id As Integer
        Public Property Nome As String

        Private _selezionata As Boolean
        Public Property Selezionata As Boolean
            Get
                Return _selezionata
            End Get
            Set(value As Boolean)
                _selezionata = value
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Selezionata)))
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    End Class
End Namespace
