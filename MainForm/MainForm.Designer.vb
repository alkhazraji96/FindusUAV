<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.btnCATIA = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnCATIA
        '
        Me.btnCATIA.Location = New System.Drawing.Point(318, 274)
        Me.btnCATIA.Name = "btnCATIA"
        Me.btnCATIA.Size = New System.Drawing.Size(157, 87)
        Me.btnCATIA.TabIndex = 0
        Me.btnCATIA.Text = "Generate Tail"
        Me.btnCATIA.UseVisualStyleBackColor = True
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(800, 450)
        Me.Controls.Add(Me.btnCATIA)
        Me.Name = "MainForm"
        Me.Text = "UAV Desgin Tool"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnCATIA As Button
End Class
