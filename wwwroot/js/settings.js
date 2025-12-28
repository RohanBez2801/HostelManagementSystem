/**
 * SETTINGS & PERSONALIZATION MODULE
 */

function initSettings() {
    const saved = localStorage.getItem('hostelSettings');
    if (saved) {
        const settings = JSON.parse(saved);
        applySettings(settings);

        // Load Branding
        if (document.getElementById('setHostelName')) {
            document.getElementById('setHostelName').value = settings.hostelName || "";
            document.getElementById('setLogoText').value = settings.logoText || "";

            // Load Financials
            document.getElementById('setCurrency').value = settings.currency || "N$";
            const fullFee = settings.fullFee || (parseFloat(settings.moeFee || 619) + parseFloat(settings.hdfFee || 0));
            document.getElementById('setFullFee').value = fullFee;
            calculateSettingsSplit();

            // Load Contact Details
            document.getElementById('setAddressPhys').value = settings.addressPhys || "";
            document.getElementById('setAddressPost').value = settings.addressPost || "";
            document.getElementById('setPhone').value = settings.phone || "";
            document.getElementById('setEmail').value = settings.email || "";

            // Load Bank Details
            document.getElementById('setBankName').value = settings.bankName || "";
            document.getElementById('setAccName').value = settings.accName || "";
            document.getElementById('setAccNo').value = settings.accNo || "";
            document.getElementById('setBranch').value = settings.branch || "";

            if (settings.logoFileName) {
                document.getElementById('logoFileName').innerText = settings.logoFileName;
            }
        }
    } else {
        // Defaults
        if (document.getElementById('setCurrency')) {
            document.getElementById('setCurrency').value = "N$";
            document.getElementById('setFullFee').value = "2119.00";
            calculateSettingsSplit();
        }
    }
}

function calculateSettingsSplit() {
    const full = parseFloat(document.getElementById('setFullFee').value) || 0;
    const moe = 619.00;
    const hdf = Math.max(0, full - moe);
    const currency = document.getElementById('setCurrency')?.value || "N$";

    const display = document.getElementById('settingsFeeSplit');
    if (display) {
        display.innerText = `MoE: ${currency} ${moe.toFixed(2)} | HDF: ${currency} ${hdf.toFixed(2)}`;
    }
}

async function previewLogo(input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        document.getElementById('logoFileName').innerText = file.name;

        const reader = new FileReader();
        reader.onload = function (e) {
            const logoData = e.target.result;
            // Store temporarily in the form dataset
            document.getElementById('brandingForm').dataset.tempLogo = logoData;
            document.getElementById('brandingForm').dataset.tempLogoName = file.name;

            // Extract color for visual feedback
            updateThemeFromLogo(logoData);

            // Show immediate preview in top nav
            const imgLogo = document.getElementById('hostelLogoImg');
            if (imgLogo) { imgLogo.src = logoData; imgLogo.style.display = 'block'; }
            document.getElementById('hostelLogoPlaceholder').style.display = 'none';
        };
        reader.readAsDataURL(file);
    }
}

async function saveSettings() {
    const logoData = document.getElementById('brandingForm').dataset.tempLogo;
    const logoFileName = document.getElementById('brandingForm').dataset.tempLogoName;

    const full = parseFloat(document.getElementById('setFullFee').value) || 0;
    const moe = 619.00;
    const hdf = Math.max(0, full - moe);

    // Build the big settings object
    const settings = {
        // Branding
        hostelName: document.getElementById('setHostelName').value,
        logoText: document.getElementById('setLogoText').value,
        logoData: logoData || (JSON.parse(localStorage.getItem('hostelSettings')) || {}).logoData,
        logoFileName: logoFileName || (JSON.parse(localStorage.getItem('hostelSettings')) || {}).logoFileName,

        // Financials
        currency: document.getElementById('setCurrency').value || "N$",
        fullFee: full,
        moeFee: moe,
        hdfFee: hdf,

        // Contact
        addressPhys: document.getElementById('setAddressPhys').value,
        addressPost: document.getElementById('setAddressPost').value,
        phone: document.getElementById('setPhone').value,
        email: document.getElementById('setEmail').value,

        // Banking
        bankName: document.getElementById('setBankName').value,
        accName: document.getElementById('setAccName').value,
        accNo: document.getElementById('setAccNo').value,
        branch: document.getElementById('setBranch').value
    };

    localStorage.setItem('hostelSettings', JSON.stringify(settings));
    applySettings(settings);
    alert("Settings saved successfully!");
}

function applySettings(settings) {
    if (!settings) return;

    // 1. Update Top Nav Branding
    const displayLogo = document.getElementById('hostelNameDisplay');
    const placeholder = document.getElementById('hostelLogoPlaceholder');
    const imgLogo = document.getElementById('hostelLogoImg');

    if (displayLogo) displayLogo.innerText = settings.hostelName || "HostelPro";

    if (settings.logoData) {
        if (imgLogo) {
            imgLogo.src = settings.logoData;
            imgLogo.style.display = 'block';
        }
        if (placeholder) placeholder.style.display = 'none';
        updateThemeFromLogo(settings.logoData);
    }

    // 2. Update Document Title
    if (settings.hostelName) document.title = `${settings.hostelName} | Dashboard`;
}

function updateThemeFromLogo(base64) {
    const img = new Image();
    img.onload = function () {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        // Simple pixel sampling center-ish
        const p = ctx.getImageData(img.width / 2, img.height / 2, 1, 1).data;
        // Logic to avoid white/transparent can be added here, simplified for now:
        if (p[3] > 128) {
            const primary = `rgb(${p[0]}, ${p[1]}, ${p[2]})`;
            document.documentElement.style.setProperty('--primary', primary);
            if (document.getElementById('colorPreview'))
                document.getElementById('colorPreview').style.background = primary;
        }
    };
    img.src = base64;
}

// --- SYSTEM TOOLS ---

async function runMigration() {
    if (!confirm("This will attempt to create missing columns in the database. Continue?")) return;

    const btn = event.target;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Running...';
    btn.disabled = true;

    try {
        const res = await fetch('/api/learner/migrate-db');
        const data = await res.json();

        let msg = "Migration Result:\n";
        if (data.log && Array.isArray(data.log)) {
            msg += data.log.join("\n");
        } else {
            msg += JSON.stringify(data);
        }
        alert(msg);
    } catch (err) {
        alert("Migration Failed: " + err.message);
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

// Add listeners
document.addEventListener('DOMContentLoaded', () => {
    initSettings();
});