// Function to set the External Website control (iframe) source URL
function updateExternalWebsiteControl(executionContext) {
    try {
        var formContext = executionContext.getFormContext();
        // Get the Name field value
        var name = formContext.getAttribute("prmtk_name").getValue();
        console.log("Name field value:", name);
        if (name) {
            // Sanitize folder name by removing special characters: "\".*:<>?/|\\`
            var sanitizedFolderName = name.replace(/[".*:<>?/|\\`]/g, '');
            // Construct the SharePoint folder URL
            var siteUrl = "https://ecagovae.sharepoint.com/sites/Procurement";
            var libraryName = "Vendors";
            var folderUrl = siteUrl + "/" + libraryName + "/" + encodeURIComponent(sanitizedFolderName);
            console.log("Folder URL:", folderUrl);
            // Set the URL in the External Website control (iframe)
            var iframeControl = formContext.getControl("IFRAME_SharePointFolderUrl");
            if (iframeControl) {
                iframeControl.setSrc(folderUrl);
            }
        }
    } catch (error) {
        console.error("Error in updateExternalWebsiteControl:", error);
        if (executionContext && executionContext.getFormContext) {
            var formContext = executionContext.getFormContext();
            formContext.ui.setFormNotification("An error occurred while updating the SharePoint folder URL.", "ERROR", "updateExternalWebsiteControl");
        }
    }
}

// Function to check for special characters in the name field
function checkspecialchar(executionContext) {
    var formContext = executionContext.getFormContext();
    var eventArgs = executionContext.getEventArgs?.(); // Use optional chaining
    var iChars = "\".*:<>?/|\\`";
    var name = formContext.getAttribute("prmtk_name").getValue();

    if (name != null) {
        for (var i = 0; i < name.length; i++) {
            if (iChars.indexOf(name.charAt(i)) !== -1) {
                var message = 'These special characters are not allowed in the name field: ".*:<>?/|\\`';
                alert(message);
                formContext.ui.setFormNotification(message, "ERROR", "2001");
                // Cancel save if possible
                if (eventArgs && typeof eventArgs.preventDefault === 'function') {
                    eventArgs.preventDefault();
                }
                return false;
            }
        }
        formContext.ui.clearFormNotification("2001");
    }
}

// Function to toggle the visibility of the External Website control based on form type
function toggleSectionByFormType(executionContext) {
    var formContext = executionContext.getFormContext();
    var formType = formContext.ui.getFormType();

    // Replace with your actual tab and section names
    var tabName = "Vendor";
    var sectionName = "Vendor_folder_control";

    var section = formContext.ui.tabs.get(tabName).sections.get(sectionName);

    if (section) {
        if (formType === 1) {
            // New Form - Hide section
            section.setVisible(false);
        } else if (formType === 2) {
            // Update Form - Show section
            section.setVisible(true);
        }
    } else {
        console.error("Section not found: " + sectionName);
    }
}


