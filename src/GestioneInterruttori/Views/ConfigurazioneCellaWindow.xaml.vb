Imports System.ComponentModel
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models
Imports GestioneInterruttori.ViewModels

Namespace Views
    ''' <summary>Finestra isolata sulla sola sezione Configurazione cella.</summary>
    Public Class ConfigurazioneCellaWindow

        Private ReadOnly _viewModel As ConfigurazioneCellaViewModel
        Private _chiusuraConfermata As Boolean

        ''' <summary>Modalità DB: interruttore già salvato, legge/scrive direttamente su ConfigurazioneCella.</summary>
        Public Sub New(percorsoDatabase As String, idInterruttore As Integer)
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New ConfigurazioneCellaViewModel(repository, idInterruttore)
            AddHandler Closing, AddressOf ConfigurazioneCellaWindow_Closing
            DataContext = _viewModel
        End Sub

        ''' <summary>Modalità in memoria: usata dentro la maschera principale, prima che l'interruttore
        ''' sia stato salvato.</summary>
        Public Sub New(percorsoDatabase As String, nomeInterruttore As String,
                       taglieDisponibili As IEnumerable(Of String),
                       lineeProdottoDisponibili As IEnumerable(Of LineaProdotto),
                       celleCorrenti As IEnumerable(Of ConfigurazioneCella))
            InitializeComponent()

            Dim db As New InterruttoriDbContext(percorsoDatabase)
            Dim repository As New InterruttoreRepository(db)
            _viewModel = New ConfigurazioneCellaViewModel(repository, nomeInterruttore, taglieDisponibili, lineeProdottoDisponibili, celleCorrenti)
            AddHandler _viewModel.Confermato, Sub()
                                                   _chiusuraConfermata = True
                                                   DialogResult = True
                                                   Close()
                                               End Sub
            AddHandler Closing, AddressOf ConfigurazioneCellaWindow_Closing
            DataContext = _viewModel
        End Sub

        ''' <summary>Valorizzato solo se aperta in modalità in memoria e confermata (DialogResult = True).</summary>
        Public ReadOnly Property CelleModificate As List(Of ConfigurazioneCella)
            Get
                Return _viewModel.CelleModificate
            End Get
        End Property

        Private Sub RimuoviGruppo_Click(sender As Object, e As RoutedEventArgs)
            Dim gruppo = TryCast(DirectCast(sender, FrameworkElement).DataContext, GruppoCella)
            If gruppo Is Nothing Then Return
            If Not DialogConferma.Chiedi(Me, "Rimuovere questa assegnazione di taglie e linee prodotto?") Then Return

            _viewModel.RimuoviGruppo(gruppo)
        End Sub

        ''' <summary>Se ci sono modifiche non salvate, chiede conferma prima di chiudere la finestra.</summary>
        Private Sub ConfigurazioneCellaWindow_Closing(sender As Object, e As CancelEventArgs)
            If _chiusuraConfermata Then Return
            If Not _viewModel.CiSonoModificheNonSalvate() Then Return

            Dim esito = DialogModificheNonSalvate.Chiedi(Me, "Ci sono modifiche non salvate. Vuoi salvarle prima di chiudere?")

            Select Case esito
                Case MessageBoxResult.Cancel
                    e.Cancel = True
                Case MessageBoxResult.Yes
                    _viewModel.SalvaCommand.Execute(Nothing)
                Case MessageBoxResult.No
                    ' Si chiude scartando le modifiche.
            End Select
        End Sub

    End Class
End Namespace
