Imports DDDSample

Public Class CustomerState
    Private _id As CustomerId
    Private _name As String
    Private _created As Boolean

    Public Sub New(events As IEnumerable(Of IEvent))
        For Each [event] In events
            Mutate([event])
        Next
    End Sub

    Public ReadOnly Property Created As Boolean
        Get
            Return _created
        End Get
    End Property
    Public Sub Mutate([event] As IEvent)
        Me.When([event])
    End Sub
    Public Sub [When]([event] As CustomerCreated)
        Me.ID = [event].Id
        Me.Name = [event].CustomerName
        Me._created = True
    End Sub

    Public Property ID As CustomerId
        Get
            Return _id
        End Get
        Protected Set(value As CustomerId)
            _id = value
        End Set
    End Property
    Public Property Name As String
        Get
            Return _name
        End Get
        Protected Set(value As String)
            _name = value
        End Set
    End Property
End Class
