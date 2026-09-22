Imports System.Windows.Input

Namespace ViewModels
    Public Class RelayCommand
        Implements ICommand

        Private ReadOnly _execute As Action
        Private ReadOnly _canExecute As Func(Of Boolean)

        Public Sub New(execute As Action, Optional canExecute As Func(Of Boolean) = Nothing)
            _execute = execute
            _canExecute = canExecute
        End Sub

        Public Function CanExecute(parameter As Object) As Boolean Implements ICommand.CanExecute
            Return _canExecute Is Nothing OrElse _canExecute()
        End Function

        Public Sub Execute(parameter As Object) Implements ICommand.Execute
            _execute()
        End Sub

        Public Event CanExecuteChanged As EventHandler Implements ICommand.CanExecuteChanged

        Public Sub RaiseCanExecuteChanged()
            RaiseEvent CanExecuteChanged(Me, EventArgs.Empty)
        End Sub
    End Class
End Namespace
