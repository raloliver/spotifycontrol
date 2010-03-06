﻿Imports System.Runtime.InteropServices

Public Class SpotifyController
    Public MySpotify As New ControllerClass
    Public Shared CurrentTrack As New Track
    Dim LastVolume As Integer
    Dim TrackChangeIndex As Integer
    Dim WithEvents Play, NextS, PrevS, BringTop, Mute, VolUp, VolDown As New Shortcut
    ' used for possible workaround the 26-second problem
    Dim WithEvents ApplicationUpdate As New System.ComponentModel.BackgroundWorker
#Region "For Aero Glass"
    <Flags()> Friend Enum DwmBlurBehindDwFlags As UInteger
        DWM_BB_ENABLE = &H1
        DWM_BB_BLURREGION = &H2
        DWM_BB_TRANSITIONONMAXIMIZED = &H4
    End Enum
    <StructLayout(LayoutKind.Sequential)> _
    Friend Structure DWM_BLURBEHIND
        Public dwFlags As DwmBlurBehindDwFlags
        Public fEnable As Boolean
        Public hRgnBlur As IntPtr
        Public fTransitionOnMaximized As Boolean
    End Structure
    <DllImport("dwmapi.dll")> _
    Friend Shared Sub DwmEnableBlurBehindWindow(ByVal hwnd As IntPtr, ByRef blurBehind As DWM_BLURBEHIND)
    End Sub
    <DllImport("dwmapi.dll", EntryPoint:="DwmIsCompositionEnabled")> _
    Friend Shared Function DwmIsCompositionEnabled(ByRef enabled As Boolean) As Integer
    End Function
#End Region
    Private Sub DownloadSomething() Handles ApplicationUpdate.DoWork
        Dim MyAutoUpdate As New AutoUpdate
        ' where the updates are downloaded from
        Dim RemotePath As String = "http://dl.dropbox.com/u/329033/SpotifyController/"
        If MyAutoUpdate.AutoUpdate(vbNullString, RemotePath) Then
            Application.Exit()
        End If
    End Sub
    Private Sub SpotifyController_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        ' call the function to unregister the hotkeys
        UnRegisterMyHotKeys()
        SettingManager.CloseServer()
    End Sub
    Private Sub SpotifyController_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ApplicationUpdate.RunWorkerAsync()
        Me.MaximizeBox = False
        TrackInfo.Show()
        TrackInfo.Hide()
        ' declare some tooltips to associate to the controls on the main window
        Dim CloseToolTip, PlayPauseToolTip, PrevToolTip, NextToolTip, LyricToolTip As New Windows.Forms.ToolTip
        CloseToolTip.SetToolTip(CloseImg, "Close SpotifyControl")
        PlayPauseToolTip.SetToolTip(PlayPauseImg, "Play/Pause the current song")
        PrevToolTip.SetToolTip(PrevImg, "Plays the previous song")
        NextToolTip.SetToolTip(NextImg, "Plays the next song")
        LyricToolTip.SetToolTip(LyricImg, "Find the lyrics for the current song")
        ' make the borders disappear
        Me.FormBorderStyle = Windows.Forms.FormBorderStyle.None
        ' if on Win7 or Vista give aero feel to the form
        If GetAeroSupport() = "Supports aero" Then
            MakeAero()
        Else
            Me.BackColor = Color.Gray
            Me.Opacity = 80 / 100
        End If
        NowPlayingBox.Text = MySpotify.GetNowplaying
        ' TODO: Find a way to get the current volume and not feed this values with shit
        VolumeControl.Value = 10
        LastVolume = 10
        ' load the hotkey settings from file
        LoadSettings()

    End Sub
    Friend Sub MakeAero()
        ' check if aero is enabled
        Dim aeroEnabled As Boolean
        SpotifyController.DwmIsCompositionEnabled(aeroEnabled)
        If aeroEnabled Then
            ' On Error Resume Next
            ' black is the color that gets transformed to glass
            Me.BackColor = Color.Black
            Me.Opacity = 1
            ' make the entire form glassy and blurry
            Dim Aux As SpotifyController.DWM_BLURBEHIND
            Aux.dwFlags = SpotifyController.DwmBlurBehindDwFlags.DWM_BB_ENABLE
            Aux.fEnable = True
            Aux.hRgnBlur = vbNull
            SpotifyController.DwmEnableBlurBehindWindow(Me.Handle, Aux)
        Else
            Me.BackColor = Color.Gray
            Me.Opacity = 80 / 100
        End If
    End Sub
    Private Sub BringToTop() Handles BringTop.Pressed
        MySpotify.BringToTop()
    End Sub
    Private Sub playpause() Handles Play.Pressed, PlayPauseImg.Click
        MySpotify.PlayPause()
        If PlayPauseImg.Tag = "Pause" Then
            PlayPauseImg.Image = My.Resources.Play
            PlayPauseImg.Tag = "Play"
        Else
            PlayPauseImg.Image = My.Resources.Pause_PNG
            PlayPauseImg.Tag = "Pause"
        End If
    End Sub
    Private Sub PlayNextBtn_Click() Handles NextImg.Click, NextS.Pressed
        MySpotify.PlayNext()
        ' Me.Text = MySpotify.GetNowplaying
        NowPlayingBox.Text = MySpotify.GetNowplaying
    End Sub

    Private Sub PlayPrevBtn_Click() Handles PrevImg.Click, PrevS.Pressed
        MySpotify.PlayPrev()
        '  Me.Text = MySpotify.GetNowplaying
        NowPlayingBox.Text = MySpotify.GetNowplaying
    End Sub

    Private Sub MuteBtn_Click() Handles MuteImg.Click, Mute.Pressed
        MySpotify.Mute()
        If LastVolume > 0 Then
            LastVolume = 0
            VolumeControl.Value = 0
        Else
            LastVolume = 10
            VolumeControl.Value = 10
        End If
    End Sub

    Private Sub VolumeControl_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles VolumeControl.Scroll
        While VolumeControl.Value <> LastVolume
            If VolumeControl.Value < LastVolume Then
                LastVolume = LastVolume - 1
                MySpotify.VolumeDown()
            Else
                MySpotify.VolumeUp()
                LastVolume = LastVolume + 1
            End If
        End While
    End Sub

    Private Sub VolUpBtn_Click() Handles VolUpImg.Click, VolUp.Pressed
        ' only if we will not go too high with the lastVolume
        If LastVolume < 10 Then
            MySpotify.VolumeUp()
            LastVolume = LastVolume + 1
            VolumeControl.Value = LastVolume
        End If
    End Sub

    Private Sub VolDownBtn_Click() Handles VolDownImg.Click, VolDown.Pressed
        ' make sure we are now to low with the volume
        If LastVolume > 0 Then
            MySpotify.VolumeDown()
            LastVolume = LastVolume - 1
            VolumeControl.Value = LastVolume
        End If
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SongCheck.Tick
        NowPlayingBox.Text = MySpotify.GetNowplaying
        'if spotify has been closed then wait for it to be opened again
        '   Debug.Print(MySpotify.SpotifyState)
        'MsgBox(CurrentTrack.TrackName & CurrentTrack.ArtistName & CurrentTrack.AlbumName)
        If MySpotify.SpotifyState = "Closed" Then
            MySpotify.LoadMe()
        End If
    End Sub
    Private Sub TextBox1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles NowPlayingBox.DoubleClick
        If MySpotify.SpotifyState <> "Closed" And NowPlayingBox.Text <> "Nothing Playing" Then
            Application.DoEvents()
            TrackInfo.LoadMe()
        End If
    End Sub
#Region "Move the window by dragging it with the mouse"
    Private x As Integer = 0
    Private y As Integer = 0


    Private Sub Me_MouseDown(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseDown, NowPlayingBox.MouseDown
        'Start to move the form.
        x = e.X
        y = e.Y
    End Sub

    Private Sub Me_MouseMove(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseMove, NowPlayingBox.MouseMove
        'Move and refresh.
        If (x <> 0 And y <> 0) Then
            Me.Location = New Point(Me.Left + e.X - x, Me.Top + e.Y - y)
            Me.Refresh()
        End If
    End Sub

    Private Sub Me_MouseUp(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles MyBase.MouseUp, NowPlayingBox.MouseUp
        'Reset the mouse point.
        x = 0
        y = 0
    End Sub
#End Region

    Friend Function GetAeroSupport() As String
        Dim strVersion As String = "No aero"
        Select Case Environment.OSVersion.Platform
            Case PlatformID.Win32NT
                Select Case Environment.OSVersion.Version.Major
                    Case 3I
                        strVersion = "No aero"
                    Case 4I
                        strVersion = "No aero"
                    Case 5I
                        strVersion = "No aero"
                    Case 6I
                        strVersion = "Supports aero"
                End Select
        End Select
        Return strVersion
    End Function

    Private Sub TextBox1_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles NowPlayingBox.TextChanged
        TrackChangeIndex = TrackChangeIndex + 1
        If NowPlayingBox.Text = "Nothing Playing" Then
            PlayPauseImg.Image = My.Resources.Play
            PlayPauseImg.Tag = "Play"
        ElseIf MySpotify.SpotifyState <> "Closed" And NowPlayingBox.Text <> vbNullString Then
            PlayPauseImg.Image = My.Resources.Pause_PNG
            Application.DoEvents()
            If TrackChangeIndex <> 1 Then
                TrackInfo.LoadMe()
            End If
            If LyricsForm.IsVisible = True Then
                ' refresh the lyrics form
                LyricsForm.LoadMe()
            End If
        ElseIf MySpotify.SpotifyState = "Closed" Then
            PlayPauseImg.Tag = "Pause"
            PlayPauseImg.Image = My.Resources.Play
        End If
    End Sub

    Private Sub CloseImg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CloseImg.Click
        Me.Close()
    End Sub

    Private Sub SettingImg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SettingImg.Click
        ' show the setting manager dialog
        SettingManager.Show() '= Windows.Forms.DialogResult.OK Then
        ' if changes were made and the user saves them
        UnRegisterMyHotKeys()
        Application.DoEvents()
        RegisterMyHotKeys()
        '   End If
    End Sub
    Private Sub RegisterMyHotKeys()
        Try
            Play.Register(1, SettingManager.MyHotKeyManager(0).MainKeyModifier, SettingManager.MyHotKeyManager(0).MainKey)
            NextS.Register(2, SettingManager.MyHotKeyManager(1).MainKeyModifier, SettingManager.MyHotKeyManager(1).MainKey)
            PrevS.Register(3, SettingManager.MyHotKeyManager(2).MainKeyModifier, SettingManager.MyHotKeyManager(2).MainKey)
            BringTop.Register(4, SettingManager.MyHotKeyManager(1).MainKeyModifier, SettingManager.MyHotKeyManager(1).MainKey)
            Mute.Register(5, SettingManager.MyHotKeyManager(3).MainKeyModifier, SettingManager.MyHotKeyManager(3).MainKey)
            VolUp.Register(6, SettingManager.MyHotKeyManager(4).MainKeyModifier, SettingManager.MyHotKeyManager(4).MainKey)
            VolDown.Register(7, SettingManager.MyHotKeyManager(5).MainKeyModifier, SettingManager.MyHotKeyManager(5).MainKey)
        Catch ex As Exception
            'MsgBox(ex.Message)
        End Try
    End Sub
    Private Sub UnRegisterMyHotKeys()
        Try
            ' UNRegister the hotkeys
            Play.Unregister(1)
            NextS.Unregister(2)
            PrevS.Unregister(3)
            BringTop.Unregister(4)
            Mute.Unregister(5)
            VolUp.Unregister(6)
            VolDown.Unregister(7)
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Private Sub LoadSettings()
        Try
            Dim SettingsReader As System.IO.StreamReader
            If IO.File.Exists(Application.StartupPath & "//Settings.ini") Then
                SettingsReader = System.IO.File.OpenText(Application.StartupPath & "//Settings.ini")
            Else
                Throw New ApplicationException("File Not Found")
            End If

            ' read all the global hotkeys values into an auxiliary HotKeyManager
            Dim AuxHotKeyManager As HotKeyManager
            For index = 0 To 5
                AuxHotKeyManager = New HotKeyManager
                AuxHotKeyManager.MainKey = SettingsReader.ReadLine()
                AuxHotKeyManager.MainKeyModifier = SettingsReader.ReadLine()
                ' Add the values to the array that contains all the keys
                SettingManager.MyHotKeyManager(index) = AuxHotKeyManager
            Next
            SettingsReader.Close()
            RegisterMyHotKeys()
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
    End Sub
    Private Sub LyricImg_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LyricImg.Click
        If MySpotify.SpotifyState <> "Closed" And NowPlayingBox.Text <> vbNullString And NowPlayingBox.Text <> "Nothing Playing" Then
            LyricsForm.LoadMe()
        End If
    End Sub
End Class
NotInheritable Class Shortcut : Inherits NativeWindow
    Private Declare Auto Function RegisterHotKey Lib "user32" (ByVal Handle As IntPtr, ByVal ID As Integer, ByVal Modifier As Integer, ByVal Key As Integer) As Integer
    Private Declare Auto Function UnregisterHotKey Lib "user32" (ByVal Handle As IntPtr, ByVal ID As Integer) As Integer
    Enum Modifier : None = 0 : Alt = 1 : Ctrl = 2 : Shift = 4 : WIN = 8 : End Enum
    Event Pressed(ByVal ID As Integer)
    Sub New()
        CreateHandle(New CreateParams)
    End Sub
    Sub Register(ByVal ID As Integer, ByVal Modifier As Modifier, ByVal Key As Keys)
        Dim result As Integer = RegisterHotKey(Handle, ID, Modifier, Key)
        If result = 0 Then
            'MsgBox("Cannot Register " & Modifier.ToString & "+" & Key.ToString & ". Already used by other application.")
        End If
    End Sub
    Sub Unregister(ByVal ID As Integer)
        UnregisterHotKey(Handle, ID)
    End Sub
    Protected Overrides Sub WndProc(ByRef M As Message)
        If M.Msg = 786 Then
            ' raise the hotkey event
            RaiseEvent Pressed(M.WParam.ToInt32)
        ElseIf M.Msg = &H31E Then
            ' the aero state has been changed
            ' repaint the main window
            SpotifyController.MakeAero()
        End If
        MyBase.WndProc(M)
    End Sub
End Class