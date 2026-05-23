Public Class MainForm
    Private tabConfiguration As TabControl
    Private btnGenerateWing As Button
    Private btnGenerateTail As Button
    Private btnResetDefaults As Button
    Private currentConfiguration As AircraftConfiguration

    Private numWingFullSpan As NumericUpDown
    Private numWingRootChord As NumericUpDown
    Private numWingTipChord As NumericUpDown
    Private cmbWingAirfoil As ComboBox
    Private numWingPointCountPerSurface As NumericUpDown
    Private numWingRibCountPerSide As NumericUpDown
    Private numWingRibThickness As NumericUpDown
    Private chkWingLighteningCutoutsEnabled As CheckBox
    Private numWingMainSparChordFraction As NumericUpDown
    Private numWingMainSparOuterDiameter As NumericUpDown
    Private numWingMainSparWallThickness As NumericUpDown
    Private numWingMainSparRibCutoutDiameter As NumericUpDown
    Private numWingAileronSpanFraction As NumericUpDown

    Private numTailPointCountPerSurface As NumericUpDown
    Private numTailDistanceOffset As NumericUpDown
    Private numTailRibThickness As NumericUpDown
    Private numTailMainSparDiameter As NumericUpDown
    Private numTailRudderClearance As NumericUpDown
    Private numHorizontalTailChord As NumericUpDown
    Private numHorizontalTailHalfSpan As NumericUpDown
    Private numHorizontalTailRibCount As NumericUpDown
    Private cmbHorizontalTailAirfoil As ComboBox
    Private numVerticalTailRootChord As NumericUpDown
    Private numVerticalTailTipChord As NumericUpDown
    Private numVerticalTailSpan As NumericUpDown
    Private numVerticalTailRibCount As NumericUpDown
    Private cmbVerticalTailAirfoil As ComboBox

    Public Sub New()
        InitializeComponent()
        BuildConfigurationUi()
        LoadDefaultConfigurationValues()
    End Sub

    Private Sub btnGenerateWing_Click(sender As Object, e As EventArgs)
        Try
            currentConfiguration = CreateConfigurationFromInputs()

            Dim validationResult As ConfigurationValidationResult =
                WingConfigurationValidator.Validate(currentConfiguration.Wing)

            If Not ConfirmConfigurationCanGenerate(validationResult) Then
                Return
            End If

            GenerateAirfoil.Run(currentConfiguration.Wing)
        Catch ex As Exception
            ShowGenerationError(ex)
        End Try
    End Sub

    Private Sub btnGenerateTail_Click(sender As Object, e As EventArgs)
        Try
            currentConfiguration = CreateConfigurationFromInputs()

            Dim validationResult As ConfigurationValidationResult =
                TailConfigurationValidator.Validate(currentConfiguration.Tail)

            If Not ConfirmConfigurationCanGenerate(validationResult) Then
                Return
            End If

            TailGenerator.Run(currentConfiguration.Tail)
        Catch ex As Exception
            ShowGenerationError(ex)
        End Try
    End Sub

    Private Sub btnResetDefaults_Click(sender As Object, e As EventArgs)
        LoadDefaultConfigurationValues()
    End Sub

    Private Sub BuildConfigurationUi()
        Me.SuspendLayout()
        Me.Controls.Clear()

        Me.Text = "UAV Design Tool"
        Me.ClientSize = New Size(1040, 720)
        Me.MinimumSize = New Size(900, 620)
        Me.StartPosition = FormStartPosition.CenterScreen

        Dim rootLayout As New TableLayoutPanel()
        rootLayout.ColumnCount = 1
        rootLayout.RowCount = 2
        rootLayout.Dock = DockStyle.Fill
        rootLayout.Padding = New Padding(12)
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))
        rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 56.0!))

        tabConfiguration = New TabControl()
        tabConfiguration.Dock = DockStyle.Fill
        tabConfiguration.TabPages.Add(CreateWingTab())
        tabConfiguration.TabPages.Add(CreateTailTab())

        Dim footerLayout As New FlowLayoutPanel()
        footerLayout.Dock = DockStyle.Fill
        footerLayout.FlowDirection = FlowDirection.RightToLeft
        footerLayout.Padding = New Padding(0, 10, 0, 0)
        footerLayout.WrapContents = False

        btnGenerateWing = New Button()
        btnGenerateWing.Text = "Generate Wing"
        btnGenerateWing.Size = New Size(132, 32)
        btnGenerateWing.Margin = New Padding(8, 0, 0, 0)
        AddHandler btnGenerateWing.Click, AddressOf btnGenerateWing_Click

        btnGenerateTail = New Button()
        btnGenerateTail.Text = "Generate Tail"
        btnGenerateTail.Size = New Size(132, 32)
        btnGenerateTail.Margin = New Padding(8, 0, 0, 0)
        AddHandler btnGenerateTail.Click, AddressOf btnGenerateTail_Click

        btnResetDefaults = New Button()
        btnResetDefaults.Text = "Reset Defaults"
        btnResetDefaults.Size = New Size(132, 32)
        btnResetDefaults.Margin = New Padding(8, 0, 0, 0)
        AddHandler btnResetDefaults.Click, AddressOf btnResetDefaults_Click

        footerLayout.Controls.Add(btnGenerateWing)
        footerLayout.Controls.Add(btnGenerateTail)
        footerLayout.Controls.Add(btnResetDefaults)

        rootLayout.Controls.Add(tabConfiguration, 0, 0)
        rootLayout.Controls.Add(footerLayout, 0, 1)

        Me.Controls.Add(rootLayout)
        Me.ResumeLayout(False)
    End Sub

    Private Function CreateWingTab() As TabPage
        Dim tabPage As New TabPage("Wing")
        tabPage.AutoScroll = True

        Dim contentGrid As TableLayoutPanel = CreateContentGrid()
        AddGroupToGrid(contentGrid, CreateWingPlanformGroup(), 0, 0)
        AddGroupToGrid(contentGrid, CreateWingAirfoilGroup(), 1, 0)
        AddGroupToGrid(contentGrid, CreateWingRibsGroup(), 0, 1)
        AddGroupToGrid(contentGrid, CreateWingSparGroup(), 1, 1)
        AddGroupToGrid(contentGrid, CreateWingAileronGroup(), 0, 2)

        tabPage.Controls.Add(contentGrid)
        Return tabPage
    End Function

    Private Function CreateTailTab() As TabPage
        Dim tabPage As New TabPage("Tail")
        tabPage.AutoScroll = True

        Dim contentGrid As TableLayoutPanel = CreateContentGrid()
        AddGroupToGrid(contentGrid, CreateTailStructureGroup(), 0, 0)
        AddGroupToGrid(contentGrid, CreateHorizontalTailGroup(), 1, 0)
        AddGroupToGrid(contentGrid, CreateVerticalTailGroup(), 0, 1)

        tabPage.Controls.Add(contentGrid)
        Return tabPage
    End Function

    Private Function CreateWingPlanformGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(3)

        numWingFullSpan = CreateNumericBox(500D, 10000D, 2, 10D)
        numWingRootChord = CreateNumericBox(50D, 2000D, 2, 5D)
        numWingTipChord = CreateNumericBox(50D, 2000D, 2, 5D)

        AddField(fieldLayout, 0, "Full span (mm)", numWingFullSpan)
        AddField(fieldLayout, 1, "Root chord (mm)", numWingRootChord)
        AddField(fieldLayout, 2, "Tip chord (mm)", numWingTipChord)

        Return CreateGroupBox("Wing Planform", fieldLayout)
    End Function

    Private Function CreateWingAirfoilGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(2)

        cmbWingAirfoil = CreateNacaComboBox()
        numWingPointCountPerSurface = CreateIntegerBox(15, 121)

        AddField(fieldLayout, 0, "Airfoil", cmbWingAirfoil)
        AddField(fieldLayout, 1, "Point count per surface", numWingPointCountPerSurface)

        Return CreateGroupBox("Wing Airfoil", fieldLayout)
    End Function

    Private Function CreateWingRibsGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(3)

        numWingRibCountPerSide = CreateIntegerBox(2, 80)
        numWingRibThickness = CreateNumericBox(0.5D, 20D, 2, 0.5D)
        chkWingLighteningCutoutsEnabled = CreateCheckBox()

        AddField(fieldLayout, 0, "Rib count per side", numWingRibCountPerSide)
        AddField(fieldLayout, 1, "Rib thickness (mm)", numWingRibThickness)
        AddField(fieldLayout, 2, "Lightening cutouts enabled", chkWingLighteningCutoutsEnabled)

        Return CreateGroupBox("Wing Ribs", fieldLayout)
    End Function

    Private Function CreateWingSparGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(4)

        numWingMainSparChordFraction = CreateNumericBox(0.15D, 0.6D, 2, 0.01D)
        numWingMainSparOuterDiameter = CreateNumericBox(2D, 150D, 2, 1D)
        numWingMainSparWallThickness = CreateNumericBox(0.2D, 25D, 2, 0.1D)
        numWingMainSparRibCutoutDiameter = CreateNumericBox(2D, 170D, 2, 1D)

        AddField(fieldLayout, 0, "Chord fraction", numWingMainSparChordFraction)
        AddField(fieldLayout, 1, "Outer diameter (mm)", numWingMainSparOuterDiameter)
        AddField(fieldLayout, 2, "Wall thickness (mm)", numWingMainSparWallThickness)
        AddField(fieldLayout, 3, "Rib cutout diameter (mm)", numWingMainSparRibCutoutDiameter)

        Return CreateGroupBox("Wing Main Spar", fieldLayout)
    End Function

    Private Function CreateWingAileronGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(1)

        numWingAileronSpanFraction = CreateNumericBox(0.15D, 0.6D, 2, 0.05D)
        AddField(fieldLayout, 0, "Span fraction of semi-span", numWingAileronSpanFraction)

        Return CreateGroupBox("Wing Aileron", fieldLayout)
    End Function

    Private Function CreateTailStructureGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(5)

        numTailDistanceOffset = CreateNumericBox(0D, 10000D, 2, 25D)
        numTailPointCountPerSurface = CreateIntegerBox(15, 121)
        numTailRibThickness = CreateNumericBox(0.5D, 20D, 2, 0.5D)
        numTailMainSparDiameter = CreateNumericBox(1D, 100D, 2, 0.5D)
        numTailRudderClearance = CreateNumericBox(0D, 1500D, 2, 1D)

        AddField(fieldLayout, 0, "Distance offset (mm)", numTailDistanceOffset)
        AddField(fieldLayout, 1, "Point count per surface", numTailPointCountPerSurface)
        AddField(fieldLayout, 2, "Rib thickness (mm)", numTailRibThickness)
        AddField(fieldLayout, 3, "Main spar diameter (mm)", numTailMainSparDiameter)
        AddField(fieldLayout, 4, "Rudder clearance (mm)", numTailRudderClearance)

        Return CreateGroupBox("Tail Structure", fieldLayout)
    End Function

    Private Function CreateHorizontalTailGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(4)

        numHorizontalTailChord = CreateNumericBox(30D, 1000D, 2, 5D)
        numHorizontalTailHalfSpan = CreateNumericBox(50D, 3000D, 2, 10D)
        numHorizontalTailRibCount = CreateIntegerBox(2, 60)
        cmbHorizontalTailAirfoil = CreateNacaComboBox()

        AddField(fieldLayout, 0, "Chord (mm)", numHorizontalTailChord)
        AddField(fieldLayout, 1, "Half span (mm)", numHorizontalTailHalfSpan)
        AddField(fieldLayout, 2, "Rib count", numHorizontalTailRibCount)
        AddField(fieldLayout, 3, "Airfoil", cmbHorizontalTailAirfoil)

        Return CreateGroupBox("Horizontal Tail", fieldLayout)
    End Function

    Private Function CreateVerticalTailGroup() As GroupBox
        Dim fieldLayout As TableLayoutPanel = CreateFieldLayout(5)

        numVerticalTailRootChord = CreateNumericBox(30D, 1000D, 2, 5D)
        numVerticalTailTipChord = CreateNumericBox(20D, 1000D, 2, 5D)
        numVerticalTailSpan = CreateNumericBox(50D, 3000D, 2, 10D)
        numVerticalTailRibCount = CreateIntegerBox(2, 60)
        cmbVerticalTailAirfoil = CreateNacaComboBox()

        AddField(fieldLayout, 0, "Root chord (mm)", numVerticalTailRootChord)
        AddField(fieldLayout, 1, "Tip chord (mm)", numVerticalTailTipChord)
        AddField(fieldLayout, 2, "Span (mm)", numVerticalTailSpan)
        AddField(fieldLayout, 3, "Rib count", numVerticalTailRibCount)
        AddField(fieldLayout, 4, "Airfoil", cmbVerticalTailAirfoil)

        Return CreateGroupBox("Vertical Tail", fieldLayout)
    End Function

    Private Function CreateContentGrid() As TableLayoutPanel
        Dim contentGrid As New TableLayoutPanel()
        contentGrid.AutoSize = True
        contentGrid.AutoSizeMode = AutoSizeMode.GrowAndShrink
        contentGrid.ColumnCount = 2
        contentGrid.Dock = DockStyle.Top
        contentGrid.Padding = New Padding(10)
        contentGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
        contentGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
        Return contentGrid
    End Function

    Private Sub AddGroupToGrid(ByVal contentGrid As TableLayoutPanel,
                               ByVal groupBox As GroupBox,
                               ByVal columnIndex As Integer,
                               ByVal rowIndex As Integer)
        While contentGrid.RowStyles.Count <= rowIndex
            contentGrid.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        End While

        contentGrid.RowCount = Math.Max(contentGrid.RowCount, rowIndex + 1)
        groupBox.Margin = New Padding(6)
        contentGrid.Controls.Add(groupBox, columnIndex, rowIndex)
    End Sub

    Private Function CreateGroupBox(ByVal title As String,
                                    ByVal fieldLayout As TableLayoutPanel) As GroupBox
        Dim groupBox As New GroupBox()
        groupBox.Text = title
        groupBox.AutoSize = True
        groupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink
        groupBox.Dock = DockStyle.Fill
        groupBox.Padding = New Padding(10, 8, 10, 10)
        groupBox.Controls.Add(fieldLayout)
        Return groupBox
    End Function

    Private Function CreateFieldLayout(ByVal rowCount As Integer) As TableLayoutPanel
        Dim fieldLayout As New TableLayoutPanel()
        fieldLayout.AutoSize = True
        fieldLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink
        fieldLayout.ColumnCount = 2
        fieldLayout.RowCount = rowCount
        fieldLayout.Dock = DockStyle.Top
        fieldLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58.0!))
        fieldLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42.0!))

        For rowIndex As Integer = 0 To rowCount - 1
            fieldLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Next

        Return fieldLayout
    End Function

    Private Sub AddField(ByVal fieldLayout As TableLayoutPanel,
                         ByVal rowIndex As Integer,
                         ByVal labelText As String,
                         ByVal inputControl As Control)
        Dim fieldLabel As New Label()
        fieldLabel.Text = labelText
        fieldLabel.AutoSize = True
        fieldLabel.Anchor = AnchorStyles.Left
        fieldLabel.Margin = New Padding(3, 7, 8, 3)

        inputControl.Margin = New Padding(3)

        fieldLayout.Controls.Add(fieldLabel, 0, rowIndex)
        fieldLayout.Controls.Add(inputControl, 1, rowIndex)
    End Sub

    Private Function CreateNumericBox(ByVal minimum As Decimal,
                                      ByVal maximum As Decimal,
                                      ByVal decimalPlaces As Integer,
                                      ByVal increment As Decimal) As NumericUpDown
        Dim numericBox As New NumericUpDown()
        numericBox.Minimum = minimum
        numericBox.Maximum = maximum
        numericBox.DecimalPlaces = decimalPlaces
        numericBox.Increment = increment
        numericBox.ThousandsSeparator = True
        numericBox.Dock = DockStyle.Fill
        Return numericBox
    End Function

    Private Function CreateIntegerBox(ByVal minimum As Integer,
                                      ByVal maximum As Integer) As NumericUpDown
        Return CreateNumericBox(CDec(minimum), CDec(maximum), 0, 1D)
    End Function

    Private Function CreateNacaComboBox() As ComboBox
        Dim comboBox As New ComboBox()
        comboBox.Dock = DockStyle.Fill
        comboBox.DropDownStyle = ComboBoxStyle.DropDown
        comboBox.Items.AddRange(New Object() {"NACA 4415", "NACA 2412", "NACA 0012"})
        Return comboBox
    End Function

    Private Function CreateCheckBox() As CheckBox
        Dim checkBox As New CheckBox()
        checkBox.AutoSize = True
        checkBox.Dock = DockStyle.Fill
        Return checkBox
    End Function

    Private Sub LoadDefaultConfigurationValues()
        Dim configuration As AircraftConfiguration = AircraftConfiguration.CreateDefault()
        currentConfiguration = configuration

        Dim wing As WingConfiguration = configuration.Wing
        Dim tail As TailConfiguration = configuration.Tail

        SetNumericValue(numWingFullSpan, wing.FullSpan)
        SetNumericValue(numWingRootChord, wing.RootChord)
        SetNumericValue(numWingTipChord, wing.TipChord)
        cmbWingAirfoil.Text = wing.Airfoil.NacaCode
        SetNumericValue(numWingPointCountPerSurface, wing.PointCountPerSurface)
        SetNumericValue(numWingRibCountPerSide, wing.Ribs.CountPerSide)
        SetNumericValue(numWingRibThickness, wing.Ribs.Thickness)
        chkWingLighteningCutoutsEnabled.Checked = wing.Ribs.LighteningCutoutsEnabled
        SetNumericValue(numWingMainSparChordFraction, wing.MainSpar.ChordFraction)
        SetNumericValue(numWingMainSparOuterDiameter, wing.MainSpar.OuterDiameter)
        SetNumericValue(numWingMainSparWallThickness, wing.MainSpar.WallThickness)
        SetNumericValue(numWingMainSparRibCutoutDiameter, wing.MainSpar.RibCutoutDiameter)
        SetNumericValue(numWingAileronSpanFraction, wing.Aileron.SpanFraction)

        SetNumericValue(numTailDistanceOffset, tail.DistanceOffset)
        SetNumericValue(numTailPointCountPerSurface, tail.PointCountPerSurface)
        SetNumericValue(numTailRibThickness, tail.RibThickness)
        SetNumericValue(numTailMainSparDiameter, tail.MainSpar.MainSparDiameter)
        SetNumericValue(numTailRudderClearance, tail.RudderClearance)

        SetNumericValue(numHorizontalTailChord, tail.HorizontalStabilizer.Chord)
        SetNumericValue(numHorizontalTailHalfSpan, tail.HorizontalStabilizer.HalfSpan)
        SetNumericValue(numHorizontalTailRibCount, tail.HorizontalStabilizer.RibCount)
        cmbHorizontalTailAirfoil.Text = tail.HorizontalStabilizer.Airfoil.NacaCode

        SetNumericValue(numVerticalTailRootChord, tail.VerticalStabilizer.RootChord)
        SetNumericValue(numVerticalTailTipChord, tail.VerticalStabilizer.TipChord)
        SetNumericValue(numVerticalTailSpan, tail.VerticalStabilizer.Span)
        SetNumericValue(numVerticalTailRibCount, tail.VerticalStabilizer.RibCount)
        cmbVerticalTailAirfoil.Text = tail.VerticalStabilizer.Airfoil.NacaCode
    End Sub

    Private Function CreateConfigurationFromInputs() As AircraftConfiguration
        Dim configuration As AircraftConfiguration = AircraftConfiguration.CreateDefault()
        configuration.Wing = CreateWingConfigurationFromInputs()
        configuration.Tail = CreateTailConfigurationFromInputs()
        Return configuration
    End Function

    Private Function CreateWingConfigurationFromInputs() As WingConfiguration
        Dim wing As WingConfiguration = WingConfiguration.CreateDefault()

        wing.FullSpan = GetDoubleValue(numWingFullSpan)
        wing.RootChord = GetDoubleValue(numWingRootChord)
        wing.TipChord = GetDoubleValue(numWingTipChord)
        wing.Airfoil = GetAirfoilValue(cmbWingAirfoil)
        wing.PointCountPerSurface = GetIntegerValue(numWingPointCountPerSurface)

        wing.Ribs.CountPerSide = GetIntegerValue(numWingRibCountPerSide)
        wing.Ribs.Thickness = GetDoubleValue(numWingRibThickness)
        wing.Ribs.LighteningCutoutsEnabled = chkWingLighteningCutoutsEnabled.Checked

        wing.MainSpar.ChordFraction = GetDoubleValue(numWingMainSparChordFraction)
        wing.MainSpar.OuterDiameter = GetDoubleValue(numWingMainSparOuterDiameter)
        wing.MainSpar.WallThickness = GetDoubleValue(numWingMainSparWallThickness)
        wing.MainSpar.RibCutoutDiameter = GetDoubleValue(numWingMainSparRibCutoutDiameter)

        wing.Aileron.SpanFraction = GetDoubleValue(numWingAileronSpanFraction)

        Return wing
    End Function

    Private Function CreateTailConfigurationFromInputs() As TailConfiguration
        Dim tail As TailConfiguration = TailConfiguration.CreateDefault()

        tail.DistanceOffset = GetDoubleValue(numTailDistanceOffset)
        tail.PointCountPerSurface = GetIntegerValue(numTailPointCountPerSurface)
        tail.RibThickness = GetDoubleValue(numTailRibThickness)
        tail.MainSpar.MainSparDiameter = GetDoubleValue(numTailMainSparDiameter)
        tail.RudderClearance = GetDoubleValue(numTailRudderClearance)

        tail.HorizontalStabilizer.Chord = GetDoubleValue(numHorizontalTailChord)
        tail.HorizontalStabilizer.HalfSpan = GetDoubleValue(numHorizontalTailHalfSpan)
        tail.HorizontalStabilizer.RibCount = GetIntegerValue(numHorizontalTailRibCount)
        tail.HorizontalStabilizer.Airfoil = GetAirfoilValue(cmbHorizontalTailAirfoil)

        tail.VerticalStabilizer.RootChord = GetDoubleValue(numVerticalTailRootChord)
        tail.VerticalStabilizer.TipChord = GetDoubleValue(numVerticalTailTipChord)
        tail.VerticalStabilizer.Span = GetDoubleValue(numVerticalTailSpan)
        tail.VerticalStabilizer.RibCount = GetIntegerValue(numVerticalTailRibCount)
        tail.VerticalStabilizer.Airfoil = GetAirfoilValue(cmbVerticalTailAirfoil)

        Return tail
    End Function

    Private Function GetDoubleValue(ByVal numericBox As NumericUpDown) As Double
        Return Convert.ToDouble(numericBox.Value)
    End Function

    Private Function GetIntegerValue(ByVal numericBox As NumericUpDown) As Integer
        Return Convert.ToInt32(numericBox.Value)
    End Function

    Private Function GetAirfoilValue(ByVal comboBox As ComboBox) As AirfoilConfiguration
        Dim airfoil As AirfoilConfiguration = Nothing
        Dim airfoilText As String = If(comboBox.Text, String.Empty).Trim()

        If NacaAirfoilParser.TryParse(airfoilText, airfoil) Then
            Return airfoil
        End If

        Return New AirfoilConfiguration(airfoilText, 0.0, 0.0, 0.12)
    End Function

    Private Function ConfirmConfigurationCanGenerate(ByVal validationResult As ConfigurationValidationResult) As Boolean
        If Not validationResult.IsValid Then
            ShowValidationErrors(validationResult)
            Return False
        End If

        If validationResult.HasWarnings AndAlso
            Not ConfirmValidationWarnings(validationResult) Then
            Return False
        End If

        Return True
    End Function

    Private Sub ShowValidationErrors(ByVal validationResult As ConfigurationValidationResult)
        SelectFirstInvalidTab(validationResult)

        Dim message As String =
            "Please fix these inputs before generation:" &
            Environment.NewLine &
            Environment.NewLine &
            FormatValidationMessages(validationResult.Errors)

        MessageBox.Show(Me,
                        message,
                        "Invalid Configuration",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
    End Sub

    Private Function ConfirmValidationWarnings(ByVal validationResult As ConfigurationValidationResult) As Boolean
        Dim message As String =
            "The configuration has warnings:" &
            Environment.NewLine &
            Environment.NewLine &
            FormatValidationMessages(validationResult.Warnings) &
            Environment.NewLine &
            Environment.NewLine &
            "Continue generation?"

        Return MessageBox.Show(Me,
                               message,
                               "Configuration Warnings",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Information,
                               MessageBoxDefaultButton.Button2) = DialogResult.Yes
    End Function

    Private Function FormatValidationMessages(ByVal messages As IEnumerable(Of ConfigurationValidationMessage)) As String
        Return String.Join(Environment.NewLine,
                           messages.Select(Function(message) "- " & message.ToString()))
    End Function

    Private Sub SelectFirstInvalidTab(ByVal validationResult As ConfigurationValidationResult)
        Dim firstError As ConfigurationValidationMessage = validationResult.Errors.FirstOrDefault()

        If firstError Is Nothing OrElse tabConfiguration Is Nothing Then
            Return
        End If

        If firstError.FieldName.StartsWith("Wing", StringComparison.OrdinalIgnoreCase) Then
            tabConfiguration.SelectedIndex = 0
        ElseIf firstError.FieldName.StartsWith("Tail", StringComparison.OrdinalIgnoreCase) Then
            tabConfiguration.SelectedIndex = 1
        End If
    End Sub

    Private Sub ShowGenerationError(ByVal ex As Exception)
        MessageBox.Show(Me,
                        "Generation failed before completion:" &
                        Environment.NewLine &
                        Environment.NewLine &
                        ex.Message,
                        "Generation Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
    End Sub

    Private Sub SetNumericValue(ByVal numericBox As NumericUpDown,
                                ByVal value As Double)
        Dim numericValue As Decimal = Convert.ToDecimal(value)

        If numericValue < numericBox.Minimum Then
            numericValue = numericBox.Minimum
        ElseIf numericValue > numericBox.Maximum Then
            numericValue = numericBox.Maximum
        End If

        numericBox.Value = numericValue
    End Sub

    Private Sub SetNumericValue(ByVal numericBox As NumericUpDown,
                                ByVal value As Integer)
        SetNumericValue(numericBox, CDbl(value))
    End Sub
End Class
