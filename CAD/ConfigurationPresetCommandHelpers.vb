Imports System.Data.SQLite
Imports System.Globalization

Friend Module ConfigurationPresetCommandHelpers
    Friend Sub AddParameter(ByVal command As SQLiteCommand,
                            ByVal name As String,
                            ByVal value As Object)
        Dim parameterValue As Object = value

        If parameterValue Is Nothing Then
            parameterValue = DBNull.Value
        End If

        command.Parameters.AddWithValue(name, parameterValue)
    End Sub

    Friend Function ReadDouble(ByVal reader As SQLiteDataReader,
                               ByVal columnName As String) As Double
        Return Convert.ToDouble(reader(columnName), CultureInfo.InvariantCulture)
    End Function

    Friend Function ReadInteger(ByVal reader As SQLiteDataReader,
                                ByVal columnName As String) As Integer
        Return Convert.ToInt32(reader(columnName), CultureInfo.InvariantCulture)
    End Function

    Friend Function ReadBoolean(ByVal reader As SQLiteDataReader,
                                ByVal columnName As String) As Boolean
        Return ReadInteger(reader, columnName) <> 0
    End Function

    Friend Function ReadString(ByVal reader As SQLiteDataReader,
                               ByVal columnName As String) As String
        Return Convert.ToString(reader(columnName), CultureInfo.InvariantCulture)
    End Function
End Module
