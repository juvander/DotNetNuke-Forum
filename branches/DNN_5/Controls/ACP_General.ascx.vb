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

Imports DotNetNuke.Entities.Portals

Namespace DotNetNuke.Modules.Forum.ACP

    ''' <summary>
    ''' This is the General Settings area from Forum Administration.
    ''' Set basic configuration items for this forum module. (mod settings)
    ''' </summary>
    ''' <remarks>The majority of how the module operates is set here.
    ''' </remarks>
    Partial Public Class General
        Inherits ForumModuleBase
        Implements Utilities.AjaxLoader.IPageLoad

#Region "Interfaces"

        ''' <summary>
        ''' This is required to replace If Page.IsPostBack = False because controls are dynamically loaded via Ajax. 
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub LoadInitialView() Implements Utilities.AjaxLoader.IPageLoad.LoadInitialView
            BindAliases()
            chkAggregatedForums.Checked = objConfig.AggregatedForums
            chkEnableThreadStatus.Checked = objConfig.EnableThreadStatus
            chkEnablePostAbuse.Checked = objConfig.EnablePostAbuse
            chkDisableHTMLPosting.Checked = objConfig.DisableHTMLPosting
            ddlPrimaryAlias.SelectedValue = objConfig.PrimaryAlias
            chkHideHomeButton.Checked = objConfig.HideHomeButton
            chkHideSearchButton.Checked = objConfig.HideSearchButton
            chkEnableJournal.Checked = objConfig.EnableJournal
        End Sub

#End Region

#Region "Event Handlers"

        ''' <summary>
        ''' Because the control is loaded dynamically, there are some things that must be handled on each and every load. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
            ' Search index date
            If Not Settings("LastIndexDate") Is Nothing Then
                Try
                    lblDateIndexed.Text = Utilities.ForumUtils.ConvertTimeZone(Utilities.ForumUtils.NumToDate(CType(Settings("LastIndexDate"), Double)), objConfig).ToString
                Catch exc As Exception
                    LogException(exc)
                    lblDateIndexed.Text = Null.NullDate.ToString
                End Try
            Else
                lblDateIndexed.Text = Null.NullDate.ToString
            End If
        End Sub

        ''' <summary>
        ''' Updates the module's configuration (module settings)
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Saves the module settings shown in this view.
        ''' </remarks>
        Protected Sub cmdUpdate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdUpdate.Click
            Try
                Dim ctlModule As New Entities.Modules.ModuleController
                ctlModule.UpdateModuleSetting(ModuleId, Constants.ENABLE_AGGREGATED_FORUM, chkAggregatedForums.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.ENABLE_THREAD_STATUS, chkEnableThreadStatus.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.ENABLE_POST_ABUSE, chkEnablePostAbuse.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.DISABLE_HTML_POSTING, chkDisableHTMLPosting.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.HIDE_HOME_BUTTON, chkHideHomeButton.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.ENABLE_JOURNAL, chkEnableJournal.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.HIDE_SEARCH_BUTTON, chkHideSearchButton.Checked.ToString)
                ctlModule.UpdateModuleSetting(ModuleId, Constants.PRIMARY_SITE_ALIAS, ddlPrimaryAlias.SelectedValue.ToString())

                Configuration.ResetForumConfig(ModuleId)

                DotNetNuke.UI.Skins.Skin.AddModuleMessage(Me, DotNetNuke.Services.Localization.Localization.GetString("lblUpdateDone.Text", Me.LocalResourceFile), Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess)
            Catch exc As Exception
                Dim s As String = exc.ToString
                s = s & " "
                ProcessModuleLoadException(Me, exc)
            End Try
        End Sub

        ''' <summary>
        ''' This resets the search indexing date.  All posts posted prior to this
        ''' date will not be added into the datastore again. 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks>Resets the search date in the database. This is to overcome performance issues when indexing module.
        ''' </remarks>
        Protected Sub cmdResetDate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdResetDate.Click
            Dim objModules As New Entities.Modules.ModuleController
            objModules.UpdateModuleSetting(ModuleId, Constants.LAST_INDEX_DATE, Utilities.ForumUtils.DateToNum(Null.NullDate).ToString)
            lblDateIndexed.Text = Utilities.ForumUtils.ConvertTimeZone(Null.NullDate, objConfig).ToString
        End Sub

#End Region

#Region "Private Methods"

        ''' <summary>
        ''' Binds a list of current portal aliases to a drop down list. 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub BindAliases()
            ddlPrimaryAlias.DataSource = PortalAliasController.Instance.GetPortalAliasesByPortalId(PortalId)
            ddlPrimaryAlias.DataTextField = "HTTPAlias"
            ddlPrimaryAlias.DataValueField = "HTTPAlias"
            ddlPrimaryAlias.DataBind()
        End Sub

#End Region

    End Class

End Namespace