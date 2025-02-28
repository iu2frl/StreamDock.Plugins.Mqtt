// property-inspector.js
const $dom = {
    action: $('#action'),
    command: $('#command'),
    subReceiver: $('#subReceiver'),
    saveBtn: $('#saveBtn')
};

const $propEvent = {
    didReceiveSettings(settings) {
        $dom.action.val(settings.action || '');
        $dom.command.val(settings.command || '');
        $dom.subReceiver.val(settings.subReceiver || '');
    },
    sendToPropertyInspector(data) { }
};

// Save settings when the save button is clicked
$dom.saveBtn.on('click', () => {
    const payload = {
        action: $dom.action.val(),
        command: $dom.command.val(),
        subReceiver: $dom.subReceiver.val()
    };
    $SD.api.setSettings($SD.uuid, payload);
});

// Load the current settings when the Property Inspector is opened
$SD.on('connected', (jsonObj) => {
    const settings = jsonObj.payload.settings;
    $propEvent.didReceiveSettings(settings);
});
