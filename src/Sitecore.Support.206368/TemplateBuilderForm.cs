
namespace Sitecore.Support.Shell.Applications.Templates.TemplateBuilder
{
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Shell.Applications.Templates.TemplateBuilder;
  using Sitecore.Shell.Framework;
  using Sitecore.Shell.Framework.Commands;
  using Sitecore.Web;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.Web.UI.WebControls.Ribbons;
  using Sitecore.Web.UI.XmlControls;
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.IO;
  using System.Text;
  using System.Web;
  using System.Web.UI;
  using System.Reflection;
  using Sitecore.Reflection;
  public class TemplateBuilderForm : Sitecore.Shell.Applications.Templates.TemplateBuilder.TemplateBuilderForm
  {
    /// <summary>
    /// Represents a ScanResult.
    /// </summary>
    private class ScanResult
    {
      /// <summary>
      /// The _display name.
      /// </summary>
      private readonly string displayName;

      /// <summary>
      /// The _field definition.
      /// </summary>
      private readonly TemplateField fieldDefinition;

      /// <summary>
      /// The _shared.
      /// </summary>
      private readonly bool shared;

      /// <summary>
      /// The _unversioned.
      /// </summary>
      private readonly bool unversioned;

      /// <summary>
      /// Gets the name of the display.
      /// </summary>
      /// <value>The name of the display.</value>
      public string DisplayName
      {
        get
        {
          return this.displayName;
        }
      }

      /// <summary>
      /// Gets the field definition.
      /// </summary>
      /// <value>The field definition.</value>
      public TemplateField FieldDefinition
      {
        get
        {
          return this.fieldDefinition;
        }
      }

      /// <summary>
      /// Gets a value indicating whether this <see cref="T:Sitecore.Shell.Applications.Templates.TemplateBuilder.TemplateBuilderForm.ScanResult" /> is shared.
      /// </summary>
      /// <value><c>true</c> if shared; otherwise, <c>false</c>.</value>
      public bool Shared
      {
        get
        {
          return this.shared;
        }
      }

      /// <summary>
      /// Gets a value indicating whether this <see cref="T:Sitecore.Shell.Applications.Templates.TemplateBuilder.TemplateBuilderForm.ScanResult" /> is unversioned.
      /// </summary>
      /// <value><c>true</c> if unversioned; otherwise, <c>false</c>.</value>
      public bool Unversioned
      {
        get
        {
          return this.unversioned;
        }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.Templates.TemplateBuilder.TemplateBuilderForm.ScanResult" /> class.
      /// </summary>
      /// <param name="fieldDefinition">
      /// The field definition.
      /// </param>
      /// <param name="displayName">
      /// Name of the display.
      /// </param>
      /// <param name="shared">
      /// if set to <c>true</c> this instance is shared.
      /// </param>
      /// <param name="unversioned">
      /// if set to <c>true</c> this instance is unversioned.
      /// </param>
      public ScanResult(TemplateField fieldDefinition, string displayName, bool shared, bool unversioned)
      {
        Assert.ArgumentNotNull(fieldDefinition, "fieldDefinition");
        Assert.ArgumentNotNull(displayName, "displayName");
        this.fieldDefinition = fieldDefinition;
        this.displayName = displayName;
        this.shared = shared;
        this.unversioned = unversioned;
      }
    }

    /// <summary>
    /// The _scan result.
    /// </summary>
    private List<TemplateBuilderForm.ScanResult> scanResult;

    /// <summary>
    /// The is read only
    /// </summary>
    private bool? isReadOnly;



    /// <summary>
    /// Raises the load event.
    /// </summary>
    /// <param name="e">
    /// The <see cref="T:System.EventArgs" /> instance containing the event data.
    /// </param>
    /// <remarks>
    /// This method notifies the server control that it should perform actions common to each HTTP
    /// request for the page it is associated with, such as setting up a database query. At this
    /// stage in the page life cycle, server controls in the hierarchy are created and initialized,
    /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
    /// property to determine whether the page is being loaded in response to a client post back,
    /// or if it is being loaded and accessed for the first time.
    /// </remarks>
    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        this.Definition = "<sitecore><section name=\"Data\" /></sitecore>";
        ItemUri itemUri = ItemUri.ParseQueryString();
        if (itemUri != null)
        {
          this.LoadTemplate(itemUri);
        }
        this.AddNewSectionText.Attributes["Value"] = Translate.Text("Add a new section");
        this.AddNewFieldText.Attributes["Value"] = Translate.Text("Add a new field");
        this.FieldTypes.Attributes["Value"] = RenderFieldTypes("Single-Line Text");
        TemplateDefinition templateDefinition = TemplateDefinition.Parse(base.Definition);
        this.TemplateID.Attributes["value"] = templateDefinition.TemplateID;
        this.Caption.Attributes["value"] = templateDefinition.DisplayName;
        this.RenderTemplate();
        this.UpdateRibbon();
        return;
      }
      this.UpdateTemplate();
    }
    /// <summary>
    /// Fixups the fields.
    /// </summary>
    /// <param name="section">
    /// The section.
    /// </param>
    private static void FixupFields(SectionDefinition section)
    {
      if (section.Fields.Count < 1)
      {
        return;
      }
      int[] array = new int[section.Fields.Count];
      for (int i = 0; i < section.Fields.Count; i++)
      {
        FieldDefinition fieldDefinition = section.Fields[i] as FieldDefinition;
        if (fieldDefinition == null)
        {
          array[i] = 0;
        }
        else
        {
          array[i] = MainUtil.GetInt(fieldDefinition.Sortorder, 0);
        }
      }
      array = new TemplateBuilderSorter().FixNumbers(array);
      for (int j = 0; j < section.Fields.Count; j++)
      {
        FieldDefinition fieldDefinition2 = section.Fields[j] as FieldDefinition;
        if (fieldDefinition2 != null)
        {
          fieldDefinition2.Sortorder = array[j].ToString();
        }
      }
    }

    /// <summary>
    /// Fixups the sections.
    /// </summary>
    /// <param name="sections">
    /// The sections.
    /// </param>
    private static void FixupSections(ArrayList sections)
    {
      if (sections.Count < 1)
      {
        return;
      }
      int[] array = new int[sections.Count];
      for (int i = 0; i < sections.Count; i++)
      {
        SectionDefinition sectionDefinition = sections[i] as SectionDefinition;
        if (sectionDefinition == null)
        {
          array[i] = 0;
        }
        else
        {
          array[i] = MainUtil.GetInt(sectionDefinition.Sortorder, 0);
        }
      }
      array = new TemplateBuilderSorter().FixNumbers(array);
      for (int j = 0; j < sections.Count; j++)
      {
        SectionDefinition sectionDefinition2 = sections[j] as SectionDefinition;
        if (sectionDefinition2 != null)
        {
          sectionDefinition2.Sortorder = array[j].ToString();
        }
      }
    }

    /// <summary>
    /// Fixups the sort order.
    /// </summary>
    /// <param name="template">
    /// The definition.
    /// </param>
    //private static void FixupSortOrder(TemplateDefinition template)
    //{
    //    TemplateBuilderForm.FixupSections(template.Sections);
    //    using (IEnumerator enumerator = template.Sections.GetEnumerator())
    //    {
    //        while (enumerator.MoveNext())
    //        {
    //            TemplateBuilderForm.FixupFields((SectionDefinition)enumerator.Current);
    //        }
    //    }
    //}

    /// <summary>
    /// Gets the control ID.
    /// </summary>
    /// <param name="controlID">
    /// The control ID.
    /// </param>
    /// <param name="postfix">
    /// The postfix.
    /// </param>
    /// <returns>
    /// The control ID with the postfix.
    /// </returns>
    private static string GetControlID(string controlID, string postfix)
    {
      Assert.ArgumentNotNullOrEmpty(controlID, "controlID");
      Assert.ArgumentNotNull(postfix, "postfix");
      return " id=\"" + controlID + postfix + "\"";
    }

    /// <summary>
    /// Loads the field.
    /// </summary>
    /// <param name="section">
    /// The section.
    /// </param>
    /// <param name="field">
    /// The field.
    /// </param>
    private static void LoadField(SectionDefinition section, TemplateFieldItem field)
    {
      Assert.ArgumentNotNull(section, "section");
      Assert.ArgumentNotNull(field, "field");
      FieldDefinition fieldDefinition = new FieldDefinition
      {
        FieldID = field.ID.ToString(),
        Name = field.Name,
        Type = field.Type,
        Source = field.Source,
        IsUnversioned = (field.IsUnversioned ? "1" : "0"),
        IsShared = (field.IsShared ? "1" : "0"),
        Sortorder = field.Sortorder.ToString()
      };
      section.AddField(fieldDefinition);
    }

    /// <summary>
    /// Loads the section.
    /// </summary>
    /// <param name="template">
    /// The template.
    /// </param>
    /// <param name="section">
    /// The section.
    /// </param>
    private static void LoadSection(TemplateDefinition template, TemplateSectionItem section)
    {
      Assert.ArgumentNotNull(template, "template");
      Assert.ArgumentNotNull(section, "section");
      SectionDefinition sectionDefinition = new SectionDefinition
      {
        SectionID = section.ID.ToString(),
        Name = section.Name,
        Sortorder = section.Sortorder.ToString()
      };
      template.AddSection(sectionDefinition);
      TemplateFieldItem[] fields = section.GetFields();
      for (int i = 0; i < fields.Length; i++)
      {
        TemplateFieldItem field = fields[i];
        TemplateBuilderForm.LoadField(sectionDefinition, field);
      }
      sectionDefinition.Fields.Sort(new FieldComparer());
    }

    /// <summary>
    /// Loads the template.
    /// </summary>
    /// <param name="templateItem">
    /// The template item.
    /// </param>
    /// <returns>
    /// The template.
    /// </returns>
    private static TemplateDefinition LoadTemplate(TemplateItem templateItem)
    {
      Assert.ArgumentNotNull(templateItem, "templateItem");
      TemplateDefinition templateDefinition = new TemplateDefinition
      {
        TemplateID = templateItem.ID.ToString(),
        DisplayName = templateItem.DisplayName
      };
      TemplateSectionItem[] sections = templateItem.GetSections();
      for (int i = 0; i < sections.Length; i++)
      {
        TemplateSectionItem section = sections[i];
        TemplateBuilderForm.LoadSection(templateDefinition, section);
      }
      return templateDefinition;
    }

    /// <summary>
    /// Renders the field.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    /// <param name="field">
    /// The field.
    /// </param>
    private void RenderField(HtmlTextWriter output, FieldDefinition field)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(field, "field");
      if (field.Deleted == "1")
      {
        return;
      }
      if (string.IsNullOrEmpty(field.ControlID))
      {
        field.ControlID = ID.NewID.ToShortID().ToString();
      }
      this.RenderField(output, field.ControlID, field.FieldID, field.Name, field.Type, field.Source, field.IsUnversioned, field.IsShared, field.Active, field.Sortorder);
    }

    /// <summary>
    /// Renders the field.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="controlID">The control ID.</param>
    /// <param name="fieldID">The field ID.</param>
    /// <param name="name">The field name.</param>
    /// <param name="type">The field type.</param>
    /// <param name="source">The source.</param>
    /// <param name="isUnversioned">The is unversioned.</param>
    /// <param name="isShared">The is shared.</param>
    /// <param name="active">if set to <c>true</c> this instance is active.</param>
    /// <param name="sortorder">The sort order.</param>
    private void RenderField(HtmlTextWriter output, string controlID, string fieldID, string name, string type, string source, string isUnversioned, string isShared, bool active, string sortorder)
    {
      Assert.ArgumentNotNull(output, "output");
      if (string.IsNullOrEmpty(controlID))
      {
        controlID = ID.NewID.ToShortID().ToString();
      }
      if (string.IsNullOrEmpty(sortorder))
      {
        sortorder = "0";
      }
      string text = string.Empty;
      source = HttpUtility.HtmlEncode(source);
      name = StringUtil.EscapeQuote(name);
      if (string.IsNullOrEmpty(name))
      {
        name = Translate.Text("Add a new field");
        text = " style=\"color:#999999\"";
      }
      #region Sitecore.Support.206368
      Item fieldItem = Client.ContentDatabase.GetItem(fieldID, this.ContentLanguage);
      bool isFieldProtected = fieldItem != null && fieldItem.Appearance.ReadOnly;
      string text2 = this.IsReadOnly || isFieldProtected ? " disabled =\"true\"" : string.Empty; //added checking "isFieldProtected"
      #endregion
      output.Write(string.Concat(new string[]
          {
                "<tr",
                TemplateBuilderForm.GetControlID(controlID, string.Empty),
                " class=\"scTableFieldRow\"",
                active ? " style=\"background:#f0f0ff\"" : string.Empty,
                ">"
          }));
      string text3 = "onchange=\"javascript:return Sitecore.TemplateBuilder.fieldChange(this,event)\" onkeyup=\"javascript:return Sitecore.TemplateBuilder.fieldChange(this,event)\" oncut=\"javascript:return Sitecore.TemplateBuilder.fieldChange(this,event)\" onpaste=\"javascript:return Sitecore.TemplateBuilder.fieldChange(this,event)\" onfocus=\"javascript:return Sitecore.TemplateBuilder.focus(this,event)\" onblur=\"javascript:return Sitecore.TemplateBuilder.blur(this,event,1)\" ";
      text3 = ((!this.IsReadOnly) ? text3 : string.Empty);
      output.Write(string.Concat(new string[]
      {
                "<td class=\"scTableFieldName\"><input",
                TemplateBuilderForm.GetControlID(controlID, "_field_name"),
                " class=\"scTableFieldNameInput\" value=\"",
                name,
                "\"",
                text,
                " ",
                text3,
                text2,
                "/>"
      }));
      output.Write(string.Concat(new string[]
      {
                "<input",
                TemplateBuilderForm.GetControlID(controlID, "_field_id"),
                " type=\"hidden\" value=\"",
                fieldID,
                "\" />"
      }));
      output.Write("<input" + TemplateBuilderForm.GetControlID(controlID, "_field_deleted") + " type=\"hidden\" />");
      output.Write(string.Concat(new string[]
      {
                "<input",
                TemplateBuilderForm.GetControlID(controlID, "_field_sortorder"),
                " type=\"hidden\" value=\"",
                sortorder,
                "\"/>"
      }));
      output.Write("</td>");
      text3 = ((!this.IsReadOnly) ? "onfocus=\"javascript:return Sitecore.TemplateBuilder.focus(this,event)\" " : string.Empty);
      output.Write(string.Concat(new string[]
      {
                "<td class=\"scTableFieldType\"><select",
                TemplateBuilderForm.GetControlID(controlID, "_field_type"),
                " class=\"scTableFieldTypeInput scCombobox\"",
                text3,
                text2,
                ">",
                TemplateBuilderForm.RenderFieldTypes(type),
                "</select></td>"
      }));
      output.Write(string.Concat(new string[]
      {
                "<td class=\"scTableFieldSource\"><input",
                TemplateBuilderForm.GetControlID(controlID, "_field_source"),
                " class=\"scTableFieldSourceInput\" value=\"",
                source,
                "\"",
                text3,
                text2,
                "></td>"
      }));
      output.Write(string.Concat(new string[]
      {
                "<td class=\"scTableFieldUnversioned\"><input",
                TemplateBuilderForm.GetControlID(controlID, "_field_unversioned"),
                " class=\"scTableFieldUnversionedInput\" type=\"checkbox\"",
                (isUnversioned == "1") ? " checked=\"checked\"" : string.Empty,
                text3,
                text2,
                "></td>"
      }));
      output.Write(string.Concat(new string[]
      {
                "<td class=\"scTableFieldShared\"><input",
                TemplateBuilderForm.GetControlID(controlID, "_field_shared"),
                " class=\"scTableFieldSharedInput\" type=\"checkbox\"",
                (isShared == "1") ? " checked=\"checked\"" : string.Empty,
                text3,
                text2,
                "></td>"
      }));
      output.Write("</tr>");
      if (active)
      {
        SheerResponse.Eval("Sitecore.TemplateBuilder.focusRow(\"" + controlID + "\", null)");
      }
    }

    /// <summary>
    /// Renders the field types.
    /// </summary>
    /// <param name="selected">
    /// The selected.
    /// </param>
    /// <returns>
    /// The field types.
    /// </returns>
    private static string RenderFieldTypes(string selected)
    {
      Assert.ArgumentNotNull(selected, "selected");
      Item item = Client.CoreDatabase.GetItem("/sitecore/system/Field Types");
      Assert.IsNotNull(item, typeof(Item), "Path \"/sitecore/system/Field Types\" not found", new object[0]);
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("<option></option>");
      foreach (Item item2 in item.Children)
      {
        stringBuilder.Append("<optgroup label=\"" + item2.GetUIDisplayName() + "\">");
        foreach (Item expr_9C in item2.Children)
        {
          string name = expr_9C.Name;
          stringBuilder.Append("<option value=\"" + name + "\"");
          if (string.Compare(name, selected, StringComparison.OrdinalIgnoreCase) == 0)
          {
            stringBuilder.Append(" selected=\"selected\"");
          }
          string uIDisplayName = expr_9C.GetUIDisplayName();
          stringBuilder.Append(">" + uIDisplayName + "</option>");
        }
        stringBuilder.Append("</optgroup>");
      }
      return stringBuilder.ToString();
    }

    /// <summary>
    /// Renders the section.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    /// <param name="section">
    /// The section.
    /// </param>
    private void RenderSection(HtmlTextWriter output, SectionDefinition section)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(section, "section");
      if (section.Deleted == "1")
      {
        return;
      }
      if (string.IsNullOrEmpty(section.ControlID))
      {
        section.ControlID = ID.NewID.ToShortID().ToString();
      }
      this.RenderSection(output, section.ControlID, section.SectionID, section.Name, section.Active, section.Sortorder);
      foreach (FieldDefinition field in section.Fields)
      {
        this.RenderField(output, field);
      }
      if (!this.IsReadOnly)
      {
        this.RenderField(output, string.Empty, string.Empty, string.Empty, "Single-Line Text", string.Empty, string.Empty, string.Empty, false, "0");
      }
    }

    /// <summary>
    /// Renders the section.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="controlID">The control ID.</param>
    /// <param name="sectionID">The section ID.</param>
    /// <param name="name">The field name.</param>
    /// <param name="active">if set to <c>true</c> this instance is active.</param>
    /// <param name="sortorder">The sort order.</param>
    private void RenderSection(HtmlTextWriter output, string controlID, string sectionID, string name, bool active, string sortorder)
    {
      Assert.ArgumentNotNull(output, "output");
      if (string.IsNullOrEmpty(controlID))
      {
        controlID = ID.NewID.ToShortID().ToString();
      }
      if (string.IsNullOrEmpty(sortorder))
      {
        sortorder = "0";
      }
      string text = string.Empty;
      if (string.IsNullOrEmpty(name))
      {
        name = Translate.Text("Add a new section");
        text = " style=\"color:#999999\"";
      }
      # region Sitecore.Support.206368
      Item sectionItem = Client.ContentDatabase.GetItem(sectionID, this.ContentLanguage);
      bool isSectionProtected = sectionItem != null && sectionItem.Appearance.ReadOnly;
      string text2 = this.IsReadOnly || isSectionProtected ? " disabled =\"true\"" : string.Empty; //added checking "isSectionProtected"
      #endregion
      output.Write(string.Concat(new string[]
          {
                "<tr",
                TemplateBuilderForm.GetControlID(controlID, string.Empty),
                " class=\"scTableSectionRow\"",
                active ? " style=\"background:#f0f0ff\"" : string.Empty,
                ">"
          }));
      output.Write("<td class=\"scTableSection\" colspan=\"5\">");
      string text3 = "onchange=\"javascript:return Sitecore.TemplateBuilder.sectionChange(this,event)\" onkeyup=\"javascript:return Sitecore.TemplateBuilder.sectionChange(this,event)\" oncut=\"javascript:return Sitecore.TemplateBuilder.sectionChange(this,event)\" onpaste=\"javascript:return Sitecore.TemplateBuilder.sectionChange(this,event)\" onfocus=\"javascript:return Sitecore.TemplateBuilder.focus(this,event)\" onblur=\"javascript:return Sitecore.TemplateBuilder.blur(this,event,0)\" ";
      text3 = ((!this.IsReadOnly) ? text3 : string.Empty);
      output.Write(string.Concat(new string[]
      {
                "<input",
                TemplateBuilderForm.GetControlID(controlID, "_section_name"),
                " class=\"scTableSectionName\" value=\"",
                name,
                "\"",
                text,
                " ",
                text3,
                text2,
                "/>"
      }));
      output.Write(string.Concat(new string[]
      {
                "<input",
                TemplateBuilderForm.GetControlID(controlID, "_section_id"),
                " type=\"hidden\" value=\"",
                sectionID,
                "\" />"
      }));
      output.Write("<input" + TemplateBuilderForm.GetControlID(controlID, "_section_deleted") + " type=\"hidden\" />");
      output.Write(string.Concat(new string[]
      {
                "<input",
                TemplateBuilderForm.GetControlID(controlID, "_section_sortorder"),
                " type=\"hidden\" value=\"",
                sortorder,
                "\"/>"
      }));
      output.Write("</td>");
      output.Write("</tr>");
      if (active)
      {
        SheerResponse.Eval("Sitecore.TemplateBuilder.focusRow(\"" + controlID + "\", null)");
      }
    }

    /// <summary>
    /// Saves the field.
    /// </summary>
    /// <param name="sectionItem">
    /// The section item.
    /// </param>
    /// <param name="field">
    /// The field.
    /// </param>
    private static void SaveField(TemplateSectionItem sectionItem, FieldDefinition field)
    {
      Assert.ArgumentNotNull(sectionItem, "sectionItem");
      Assert.ArgumentNotNull(field, "field");
      TemplateFieldItem templateFieldItem = null;
      if (!string.IsNullOrEmpty(field.FieldID))
      {
        templateFieldItem = sectionItem.GetField(ID.Parse(field.FieldID));
      }
      if (field.Deleted == "1")
      {
        if (templateFieldItem != null)
        {
          templateFieldItem.InnerItem.Recycle();
          return;
        }
      }
      else
      {
        if (templateFieldItem == null)
        {
          templateFieldItem = sectionItem.AddField(field.Name);
          field.FieldID = templateFieldItem.ID.ToString();
        }
        templateFieldItem.InnerItem.Editing.BeginEdit();
        templateFieldItem.InnerItem.Name = StringUtil.GetString(new string[]
        {
                    field.Name
        });
        templateFieldItem.Type = StringUtil.GetString(new string[]
        {
                    field.Type
        });
        templateFieldItem.Source = StringUtil.GetString(new string[]
        {
                    field.Source
        });
        templateFieldItem.InnerItem[TemplateFieldIDs.Unversioned] = ((StringUtil.GetString(new string[]
        {
                    field.IsUnversioned
        }) == "1") ? "1" : string.Empty);
        templateFieldItem.InnerItem[TemplateFieldIDs.Shared] = ((StringUtil.GetString(new string[]
        {
                    field.IsShared
        }) == "1") ? "1" : string.Empty);
        templateFieldItem.InnerItem.Appearance.Sortorder = MainUtil.GetInt(field.Sortorder, 0);
        templateFieldItem.InnerItem.Editing.EndEdit();
      }
    }

    /// <summary>
    /// Saves the section.
    /// </summary>
    /// <param name="templateItem">
    /// The template item.
    /// </param>
    /// <param name="section">
    /// The section.
    /// </param>
    private static void SaveSection(TemplateItem templateItem, SectionDefinition section)
    {
      Assert.ArgumentNotNull(templateItem, "templateItem");
      Assert.ArgumentNotNull(section, "section");
      TemplateSectionItem templateSectionItem = null;
      if (!string.IsNullOrEmpty(section.SectionID))
      {
        templateSectionItem = templateItem.GetSection(ID.Parse(section.SectionID), false);
      }
      if (section.Deleted == "1")
      {
        if (templateSectionItem != null)
        {
          templateSectionItem.InnerItem.Recycle();
          return;
        }
      }
      else
      {
        if (templateSectionItem == null)
        {
          templateSectionItem = templateItem.AddSection(section.Name, false);
          section.SectionID = templateSectionItem.ID.ToString();
        }
        templateSectionItem.InnerItem.Editing.BeginEdit();
        templateSectionItem.InnerItem.Name = StringUtil.GetString(new string[]
        {
                    section.Name
        });
        templateSectionItem.InnerItem.Appearance.Sortorder = MainUtil.GetInt(section.Sortorder, 0);
        templateSectionItem.InnerItem.Editing.EndEdit();
        foreach (FieldDefinition field in section.Fields)
        {
          TemplateBuilderForm.SaveField(templateSectionItem, field);
        }
      }
    }

    /// <summary>
    /// Scans the field.
    /// </summary>
    /// <param name="sectionItem">
    /// The section item.
    /// </param>
    /// <param name="field">
    /// The field.
    /// </param>
    /// <param name="result">
    /// The result.
    /// </param>
    private static void ScanField(TemplateSectionItem sectionItem, FieldDefinition field, List<TemplateBuilderForm.ScanResult> result)
    {
      Assert.ArgumentNotNull(sectionItem, "sectionItem");
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(result, "result");
      if (field.Deleted != "1" && !string.IsNullOrEmpty(field.FieldID))
      {
        bool flag = StringUtil.GetString(new string[]
        {
                    field.IsShared
        }) == "1";
        bool flag2 = StringUtil.GetString(new string[]
        {
                    field.IsUnversioned
        }) == "1";
        TemplateFieldItem field2 = sectionItem.GetField(ID.Parse(field.FieldID));
        if (field2 != null && (field2.Shared != flag || field2.Unversioned != flag2))
        {
          Template expr_C1 = TemplateManager.GetTemplate(sectionItem.Template.ID, sectionItem.Database);
          Assert.IsNotNull(expr_C1, typeof(Template));
          TemplateField field3 = expr_C1.GetField(field2.ID);
          Assert.IsNotNull(field3, typeof(TemplateField));
          result.Add(new TemplateBuilderForm.ScanResult(field3, field2.DisplayName, flag, flag2));
        }
      }
    }

    /// <summary>
    /// Scans the section for Shared and Unversioned changes.
    /// </summary>
    /// <param name="templateItem">
    /// The template item.
    /// </param>
    /// <param name="section">
    /// The section.
    /// </param>
    /// <param name="result">
    /// The result.
    /// </param>
    private static void ScanSection(TemplateItem templateItem, SectionDefinition section, List<TemplateBuilderForm.ScanResult> result)
    {
      Assert.ArgumentNotNull(templateItem, "templateItem");
      Assert.ArgumentNotNull(section, "section");
      Assert.ArgumentNotNull(result, "result");
      if (section.Deleted != "1" && !string.IsNullOrEmpty(section.SectionID))
      {
        TemplateSectionItem section2 = templateItem.GetSection(ID.Parse(section.SectionID));
        if (section2 != null)
        {
          foreach (FieldDefinition field in section.Fields)
          {
            TemplateBuilderForm.ScanField(section2, field, result);
          }
        }
      }
    }

    /// <summary>
    /// Gets the template item.
    /// </summary>
    /// <returns>
    /// The template item.
    /// </returns>
    private TemplateItem GetTemplateItem()
    {
      TemplateItem result = null;
      TemplateDefinition templateDefinition = TemplateDefinition.Parse(this.Definition);
      if (!string.IsNullOrEmpty(templateDefinition.TemplateID))
      {
        result = Client.ContentDatabase.GetItem(templateDefinition.TemplateID, this.ContentLanguage);
      }
      return result;
    }

    /// <summary>
    /// Loads the template.
    /// </summary>
    /// <param name="uri">
    /// The item URI.
    /// </param>
    private void LoadTemplate(ItemUri uri)
    {
      Assert.ArgumentNotNull(uri, "uri");
      Item item = Database.GetItem(uri);
      if (item == null)
      {
        return;
      }
      TemplateDefinition templateDefinition = TemplateBuilderForm.LoadTemplate(item);
      this.Definition = templateDefinition.ToXml();
    }

    /// <summary>
    /// Renders the template.
    /// </summary>
    private void RenderTemplate()
    {
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      TemplateDefinition templateDefinition = TemplateDefinition.Parse(this.Definition);
      htmlTextWriter.Write("<table id=\"Table\" class=\"scListControl\" cellpadding=\"0\" cellspacing=\"0\">");
      htmlTextWriter.Write("<tr class=\"scTableHeader\">");
      htmlTextWriter.Write(string.Concat(new string[]
      {
                "<td title=\"",
                Translate.Text("The name of the field or section"),
                "\" class=\"scTableFieldNameHeader\">",
                Translate.Text("Name"),
                "</td>"
      }));
      htmlTextWriter.Write(string.Concat(new string[]
      {
                "<td title=\"",
                Translate.Text("The type of the field"),
                "\" class=\"scTableFieldTypeHeader\">",
                Translate.Text("Type"),
                "</td>"
      }));
      htmlTextWriter.Write(string.Concat(new string[]
      {
                "<td title=\"",
                Translate.Text("The Source of the field (Depends on the Field Type)"),
                "\" class=\"scTableFieldSourceHeader\">",
                Translate.Text("Source"),
                "</td>"
      }));
      htmlTextWriter.Write(string.Concat(new string[]
      {
                "<td title=\"",
                Translate.Text("The content of the field Will be shared among every versions in every language."),
                "\" class=\"scTableFieldUnversionedHeader\">",
                Translate.Text("Unversioned"),
                "</td>"
      }));
      htmlTextWriter.Write(string.Concat(new string[]
      {
                "<td title=\"",
                Translate.Text("The content of the field Will be shared among every version in every language."),
                "\" class=\"scTableFieldSharedHeader\">",
                Translate.Text("Shared"),
                "</td>"
      }));
      htmlTextWriter.Write("</tr>");
      foreach (SectionDefinition section in templateDefinition.Sections)
      {
        this.RenderSection(htmlTextWriter, section);
      }
      if (!this.IsReadOnly)
      {
        this.RenderSection(htmlTextWriter, string.Empty, string.Empty, string.Empty, false, "0");
      }
      htmlTextWriter.Write("</table>");
      this.TemplatePanel.InnerHtml = htmlTextWriter.InnerWriter.ToString();
      this.Definition = templateDefinition.ToXml();
    }

    /// <summary>
    /// Saves the template.
    /// The template will be saved in the default language when the two following conditions are met:
    /// 1. User has item:write access
    /// 2. User has language:write access in the selected language.
    /// </summary>
    private void SaveTemplate()
    {
      TemplateItem templateItem = this.GetTemplateItem() ?? Client.ContentDatabase.GetItem(ItemIDs.TemplateRoot, this.ContentLanguage);
      Error.AssertItemFound(templateItem, "/sitecore/templates");
      if (!templateItem.InnerItem.Access.CanWriteLanguage())
      {
        SheerResponse.Alert("The current user does not have write access to the current language. User:{0},  Item: {1}.", new string[]
        {
                    Context.GetUserName(),
                    templateItem.InnerItem.ID.ToString()
        });
        return;
      }
      TemplateDefinition templateDefinition = TemplateDefinition.Parse(this.Definition);
      TemplateItem templateItem2 = null;
      if (!string.IsNullOrEmpty(templateDefinition.TemplateID))
      {
        templateItem2 = Client.ContentDatabase.GetItem(templateDefinition.TemplateID, this.ContentLanguage);
      }
      Type tp = typeof(TemplateBuilderForm);
      MethodInfo minfo = tp.GetMethod("FixupSortOrder", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
      minfo.Invoke(null, new object[] { templateDefinition });
      //TemplateBuilderForm.FixupSortOrder(templateDefinition);
      try
      {
        if (templateItem2 == null)
        {
          Item item = Client.ContentDatabase.GetItem(ItemIDs.TemplateRoot);
          Error.AssertItemFound(item, "/sitecore/templates");
          Item item2 = item.Children["User Defined"];
          if (item2 != null)
          {
            item = item2;
          }
          templateItem2 = Client.ContentDatabase.Templates.CreateTemplate("New Template", item);
          Assert.IsNotNull(templateItem2, "Failed to create template.");
          templateDefinition.TemplateID = templateItem2.ID.ToString();
        }
        foreach (SectionDefinition section in templateDefinition.Sections)
        {
          TemplateBuilderForm.SaveSection(templateItem2, section);
        }
      }
      finally
      {
        this.Definition = templateDefinition.ToXml();
        this.RenderTemplate();
      }
      Context.ClientPage.Modified = false;
      Log.Audit(this, "Save template: {0}", new string[]
      {
                AuditFormatter.FormatItem(templateItem2.InnerItem)
      });
      SheerResponse.Eval("try {scForm.getParentForm().postRequest(\"\",\"\",\"\",\"item:refreshchildren(id=" + templateItem2.ID.ToShortID() + ")\") } catch(e) { }");
    }

    /// <summary>
    /// Scans the current template for Shared and Unversioned changes.
    /// </summary>
    /// <returns>
    /// Returns the list.
    /// </returns>
    private List<TemplateBuilderForm.ScanResult> Scan()
    {
      List<TemplateBuilderForm.ScanResult> result = new List<TemplateBuilderForm.ScanResult>();
      TemplateDefinition templateDefinition = TemplateDefinition.Parse(this.Definition);
      if (!string.IsNullOrEmpty(templateDefinition.TemplateID))
      {
        TemplateItem templateItem = Client.ContentDatabase.GetItem(templateDefinition.TemplateID);
        if (templateItem != null)
        {
          foreach (SectionDefinition section in templateDefinition.Sections)
          {
            TemplateBuilderForm.ScanSection(templateItem, section, result);
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Updates the ribbon.
    /// </summary>
    private void UpdateRibbon()
    {
      Ribbon ribbon = new Ribbon
      {
        ID = "Ribbon"
      };
      TemplateItem templateItem = this.GetTemplateItem();
      ribbon.CommandContext = new CommandContext(templateItem);
      ribbon.ShowContextualTabs = false;
      Item item = Client.CoreDatabase.GetItem("/sitecore/content/Applications/Templates/Template Builder/Ribbon");
      Error.AssertItemFound(item, "/sitecore/content/Applications/Templates/Template Builder/Ribbon");
      ribbon.CommandContext.Parameters["Ribbon.RenderTabs"] = "true";
      ribbon.CommandContext.Parameters["Ribbon.RenderAsContextual"] = "true";
      ribbon.CommandContext.Parameters["Ribbon.RenderContextualStripTitles"] = "true";
      ribbon.CommandContext.RibbonSourceUri = item.Uri;
      this.RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
      SheerResponse.Eval("scUpdateRibbonProxy('Ribbon', 'Ribbon')");
    }

    /// <summary>
    /// Updates the template.
    /// </summary>
    private void UpdateTemplate()
    {
      string @string = StringUtil.GetString(new string[]
      {
                Context.ClientPage.ClientRequest.Form["Active"]
      });
      string strB = Translate.Text("Add a new section");
      string strB2 = Translate.Text("Add a new field");
      TemplateDefinition templateDefinition = new TemplateDefinition
      {
        TemplateID = StringUtil.GetString(new string[]
          {
                    Context.ClientPage.ClientRequest.Form["TemplateID"]
          })
      };
      string[] arg_B0_0 = StringUtil.GetString(new string[]
      {
                Context.ClientPage.ClientRequest.Form["Structure"]
      }).Split(new char[]
      {
                ','
      });
      SectionDefinition sectionDefinition = null;
      string[] array = arg_B0_0;
      for (int i = 0; i < array.Length; i++)
      {
        string text = array[i];
        string text2 = Context.ClientPage.ClientRequest.Form[text + "_section_name"];
        if (text2 != null)
        {
          sectionDefinition = null;
          text2 = StringUtil.GetString(new string[]
          {
                        text2
          }).Trim();
          if (!string.IsNullOrEmpty(text2) && string.Compare(text2, strB, StringComparison.InvariantCultureIgnoreCase) != 0)
          {
            sectionDefinition = new SectionDefinition
            {
              Name = text2,
              ControlID = text,
              SectionID = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_section_id"]
                }),
              Deleted = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_section_deleted"]
                }),
              Sortorder = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_section_sortorder"]
                })
            };
            templateDefinition.AddSection(sectionDefinition);
            if (text == @string)
            {
              sectionDefinition.Active = true;
            }
          }
        }
        else if (sectionDefinition != null)
        {
          text2 = StringUtil.GetString(new string[]
          {
                        Context.ClientPage.ClientRequest.Form[text + "_field_name"]
          }).Trim();
          if (!string.IsNullOrEmpty(text2) && string.Compare(text2, strB2, StringComparison.InvariantCultureIgnoreCase) != 0)
          {
            FieldDefinition fieldDefinition = new FieldDefinition
            {
              Name = text2,
              ControlID = text,
              FieldID = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_field_id"]
                }),
              Type = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_field_type"]
                }),
              Source = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_field_source"]
                }),
              IsUnversioned = ((!string.IsNullOrEmpty(Context.ClientPage.ClientRequest.Form[text + "_field_unversioned"])) ? "1" : "0"),
              IsShared = ((!string.IsNullOrEmpty(Context.ClientPage.ClientRequest.Form[text + "_field_shared"])) ? "1" : "0"),
              Deleted = ((!string.IsNullOrEmpty(Context.ClientPage.ClientRequest.Form[text + "_field_deleted"])) ? "1" : "0"),
              Sortorder = StringUtil.GetString(new string[]
                {
                                Context.ClientPage.ClientRequest.Form[text + "_field_sortorder"]
                })
            };
            sectionDefinition.AddField(fieldDefinition);
            if (text == @string)
            {
              fieldDefinition.Active = true;
            }
          }
        }
      }
      this.Definition = templateDefinition.ToXml();
    }
    internal class FieldComparer : IComparer
    {
      /// <summary>
      /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
      /// </summary>
      /// <param name="x">The first object to compare.</param>
      /// <param name="y">The second object to compare.</param>
      /// <returns>
      /// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
      /// </returns>
      /// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
      public int Compare(object x, object y)
      {
        FieldDefinition fieldDefinition = x as FieldDefinition;
        FieldDefinition fieldDefinition2 = y as FieldDefinition;
        if (fieldDefinition == null || fieldDefinition2 == null)
        {
          return 0;
        }
        int @int = MainUtil.GetInt(fieldDefinition.Sortorder, 0);
        int int2 = MainUtil.GetInt(fieldDefinition2.Sortorder, 0);
        if (@int != int2)
        {
          return @int - int2;
        }
        string name = fieldDefinition.Name;
        string name2 = fieldDefinition2.Name;
        if (name.Length > 0 && name2.Length > 0)
        {
          if (name[0] == '_' && name2[0] != '_')
          {
            return 1;
          }
          if (name2[0] == '_' && name[0] != '_')
          {
            return -1;
          }
        }
        return name.CompareTo(name2);
      }
    }
  }
}


