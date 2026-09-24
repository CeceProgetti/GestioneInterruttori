Imports System.ComponentModel

Namespace ViewModels
    ''' <summary>Una taglia (tra quelle già scelte nella maschera principale), selezionabile a chip.</summary>
    Public Class TagliaSelezionabile
        Implements INotifyPropertyChanged

        Public Property Taglia As String

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
