'
' DotNetNukeŽ - http://www.dotnetnuke.com
' Copyright (c) 2002-2011
' by DotNetNuke Corporation
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'
Option Strict On
Option Explicit On

Imports DotNetNuke.Modules.Forum.Utilities

Namespace DotNetNuke.Modules.Forum

    ''' <summary>
    ''' Allows a moderator/admin to move a thread to a new forum.
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Public MustInherit Class ThreadMove
        Inherits ForumModuleBase
        Implements Entities.Modules.IActionable

#Region "Private Members"

        Private _ModeratorReturn As Boolean = False

#End Region

#Region "Optional Interfaces"

        ''' <summary>
        ''' Gets a list of module actions available to the user to provide it to DNN core.
        ''' </summary>
        ''' <value></value>
        ''' <returns>The collection of module actions available to the user</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ModuleActions() As Entities.Modules.Actions.ModuleActionCollection Implements Entities.Modules.IActionable.ModuleActions
            Get
                Return Utilities.ForumUtils.PerUserModuleActions(objConfig, Me)
            End Get
        End Property

#End Region

#Region "Properties"

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private ReadOnly Property ThreadID() As Integer
            Get
                If HttpContext.Current.Request.QueryString("threadid") Is Nothing Then
                    HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
                    Return -1
                Else
                    Return Convert.ToInt32(HttpContext.Current.Request.QueryString("threadid"))
                End If
            End Get
        End Property

#End Region

#Region "Enums"

        ''' <summary>
        ''' The image type to use to represent the various levels in the treeview. 
        ''' </summary>
        ''' <remarks></remarks>
        Private Enum eImageType
            Group = 0
            Forum = 1
        End Enum

#End Region

#Region "Event Handlers"

        ''' <summary>
        ''' Loads page settings.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' </remarks>
        Protected Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
            Try
                If Not HttpContext.Current.Request.IsAuthenticated Then
                    HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
                End If

                Dim objSecurity As New Forum.ModuleSecurity(ModuleId, TabId, -1, UserId)

                If Not (objSecurity.IsModerator) Then
                    ' they don't belong here
                    HttpContext.Current.Response.Redirect(Utilities.Links.UnAuthorizedLink(), True)
                End If

                Dim DefaultPage As CDefault = DirectCast(Page, CDefault)
                ForumUtils.LoadCssFile(DefaultPage, objConfig)

                If Page.IsPostBack = False Then
                    If Not Request.UrlReferrer Is Nothing Then
                        cmdCancel.NavigateUrl = Request.UrlReferrer.ToString()
                    Else
                        cmdCancel.NavigateUrl = NavigateURL()
                    End If

                    Dim cntThread As New ThreadController()
                    Dim objThread As New ThreadInfo
                    objThread = cntThread.GetThread(ThreadID)

                    lblSubject.Text = objThread.Subject
                    txtOldForum.Text = objThread.ContainingForum.Name

                    ' Treeview forum viewer
                    ForumTreeview.PopulateTelerikTree(objConfig, rtvForums, UserId, TabId)
                    SelectDefaultForumTree(objThread)

                    If Not Request.UrlReferrer Is Nothing Then
                        ViewState("UrlReferrer") = Request.UrlReferrer.ToString()
                    End If
                End If

            Catch exc As Exception
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        ''' <summary>
        ''' Moves the thread to a new forum, unless user is attempting to move
        ''' the thread to the same forum.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' </remarks>
        Protected Sub cmdMove_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdMove.Click
            Try
                ' See if user selected a forum to move thread to.
                Dim newForumID As Integer = -1

                For Each objNode As Telerik.Web.UI.RadTreeNode In Me.rtvForums.CheckedNodes
                    Dim strType As String = objNode.Value.Substring(0, 1)
                    Dim iID As Integer = CInt(objNode.Value.Substring(1, objNode.Value.Length - 1))

                    newForumID = iID
                    Exit For
                Next
                ' If the new forumID is the same as the old ForumID
                If (newForumID = ForumID) Then
                    ' Show user error message
                    DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, DotNetNuke.Services.Localization.Localization.GetString("OldForumSeleted.Text", Me.LocalResourceFile), Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
                    Exit Sub
                Else
                    If newForumID = -1 Then
                        DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, DotNetNuke.Services.Localization.Localization.GetString("NoForumSelected.Text", Me.LocalResourceFile), Skins.Controls.ModuleMessage.ModuleMessageType.RedError)
                        Exit Sub
                    Else
                        ' build notes (old forum, new forum, say post was moved in body (basically email body text)
                        Dim Notes As String = "Thread Move"

                        Dim tCont As ThreadController
                        tCont = New ThreadController()
                        Dim tInfo As ThreadInfo
                        tInfo = tCont.GetThread(ThreadID)
                        ' return to new forum page
                        Dim strURL As String = Utilities.Links.ContainerViewThreadLink(PortalId, TabId, newForumID, ThreadID, tInfo.Subject)
                        Dim ctlThread As New ThreadController
                        ' lets get existing info on thread
                        Dim objThread As ThreadInfo
                        objThread = ctlThread.GetThread(ThreadID)

                        'Dim MyProfileUrl As String = Utils.MySettingsLink(TabId, ModuleId)
                        Dim MyProfileUrl As String = Utilities.Links.UCP_UserLinks(TabId, ModuleId, UserAjaxControl.Tracking, PortalSettings)
                        Dim cntForum As New ForumController()
                        Dim objForum As ForumInfo = cntForum.GetForumItemCache(newForumID)

                        ctlThread.MoveThread(ThreadID, newForumID, UserId, Notes, objForum.ParentID)

                        Forum.Components.Utilities.Caching.UpdateThreadCache(ThreadID, objThread.ForumID, objThread.ContainingForum.GroupID, ModuleId, objThread.ContainingForum.ParentID)

                        If Not (ForumID = newForumID) Then
                            Forum.Components.Utilities.Caching.UpdateForumCache(newForumID, objForum.GroupID, ModuleId, objForum.ParentID)
                        End If

                        ' Handle sending emails 
                        If chkEmailUsers.Checked And objConfig.MailNotification Then
                            Utilities.ForumUtils.SendForumMail(ThreadID, strURL, ForumEmailType.UserThreadMoved, Notes, objConfig, MyProfileUrl, PortalId)
                        End If

                        Dim objPost As PostInfo
                        Dim PostCnt As New PostController
                        objPost = PostCnt.GetPostInfo(ThreadID, PortalId)

                        Response.Redirect(GetReturnURL(objPost), False)
                    End If
                End If
            Catch ex As Exception
                LogException(ex)
            End Try
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Used to select the user's default forum in the tree (if applicable)
        ''' </summary>
        ''' <param name="objThreadInfo">ThreadInfo</param>
        ''' <remarks>
        ''' </remarks>
        Private Sub SelectDefaultForumTree(ByVal objThreadInfo As ThreadInfo)
            Dim SelectedForumID As Integer
            SelectedForumID = objThreadInfo.ForumID

            Dim node As Telerik.Web.UI.RadTreeNode = rtvForums.FindNodeByValue("F" + SelectedForumID.ToString)
            node.Selected = True
            node.ParentNode.Expanded = True
            node.Checked = True
        End Sub

        ''' <summary>
        ''' Gets the URL to return the user too.
        ''' </summary>
        ''' <param name="Post"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetReturnURL(ByVal Post As PostInfo) As String
            Dim url As String

            ' This only needs to be handled for moderators (which are trusted no matter what, so it only happens at this point)
            Dim Security As New Forum.ModuleSecurity(ModuleId, TabId, Post.ForumID, UserId)

            If Security.IsForumModerator Then
                ' Check for querystring parameter to make sure this is where they came from.  Even if a direct link was typed in (should not happen) no damage will be done
                If (Not Request.QueryString("moderatorreturn") Is Nothing) Then
                    If Request.QueryString("moderatorreturn") = "1" Then
                        _ModeratorReturn = True
                    End If
                End If
            End If

            If _ModeratorReturn Then
                If Not ViewState("UrlReferrer") Is Nothing Then
                    url = (CType(ViewState("UrlReferrer"), String))
                Else
                    ' behave as before (normal usage)
                    url = Utilities.Links.ContainerViewPostLink(PortalId, TabId, Post.ForumID, Post.PostID, Post.Subject)
                End If
            Else
                ' behave as before (normal usage)
                url = Utilities.Links.ContainerViewPostLink(PortalId, TabId, Post.ForumID, Post.PostID, Post.Subject)
            End If

            Return url
        End Function

#End Region

    End Class

End Namespace