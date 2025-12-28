/**
 * SETTINGS & PERSONALIZATION MODULE (Backend Integration)
 */

async function initSettings() {
    try {
        const response = await fetch('/api/settings');
        // Check content type to avoid parsing HTML as JSON
        const contentType = response.headers.get("content-type");
        if (response.ok && contentType && contentType.includes("application/json")) {
            const settings = await response.json();
            sessionStorage.setItem('hostelSettings', JSON.stringify(settings));
            applySettings(settings);
            populateForm(settings);
        } else {
            console.warn("Settings API not available or returned error.");
        }
    } catch (err) {
        console.error("Error loading settings:", err);
    }
}

function populateForm(settings) {
    if (!document.getElementById('setHostelName')) return;

    // Branding
    document.getElementById('setHostelName').value = settings.HostelName || "";
    document.getElementById('setLogoText').value = settings.LogoText || "";

    // Financials
    document.getElementById('setCurrency').value = settings.Currency || "N$";
    const fullFee = settings.FullFee || (parseFloat(settings.MoeFee || 619) + parseFloat(settings.HdfFee || 0));
    document.getElementById('setFullFee').value = fullFee;
    calculateSettingsSplit();

    // Contact Details
    document.getElementById('setAddressPhys').value = settings.AddressPhys || "";
    document.getElementById('setAddressPost').value = settings.AddressPost || "";
    document.getElementById('setPhone').value = settings.Phone || "";
    document.getElementById('setEmail').value = settings.Email || "";

    // Bank Details
    document.getElementById('setBankName').value = settings.BankName || "";
    document.getElementById('setAccName').value = settings.AccName || "";
    document.getElementById('setAccNo').value = settings.AccNo || "";
    document.getElementById('setBranch').value = settings.Branch || "";

    // License Info
    if (settings.LicenseExpiry) {
        const expiry = new Date(settings.LicenseExpiry);
        const now = new Date();
        const daysLeft = Math.ceil((expiry - now) / (1000 * 60 * 60 * 24));

        const licStatus = document.getElementById('licenseStatus');
        const licInfo = document.getElementById('licenseInfo');

        if (licStatus) {
            if (daysLeft > 0) {
                licStatus.innerHTML = `<span class="badge-success">Active</span>`;
                licInfo.innerText = `Valid until: ${expiry.toLocaleDateString()} (${daysLeft} days remaining)`;
            } else {
                licStatus.innerHTML = `<span class="badge-warning" style="background:red; color:white">Expired</span>`;
                licInfo.innerText = `Expired on: ${expiry.toLocaleDateString()}`;
            }
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
            document.getElementById('brandingForm').dataset.tempLogo = logoData;

            updateThemeFromLogo(logoData);

            const imgLogo = document.getElementById('hostelLogoImg');
            if (imgLogo) { imgLogo.src = logoData; imgLogo.style.display = 'block'; }
            document.getElementById('hostelLogoPlaceholder').style.display = 'none';
        };
        reader.readAsDataURL(file);
    }
}

async function saveSettings() {
    const logoData = document.getElementById('brandingForm').dataset.tempLogo;

    const full = parseFloat(document.getElementById('setFullFee').value) || 0;
    const moe = 619.00;
    const hdf = Math.max(0, full - moe);

    const settings = {
        HostelName: document.getElementById('setHostelName').value,
        LogoText: document.getElementById('setLogoText').value,
        Currency: document.getElementById('setCurrency').value || "N$",
        FullFee: full,
        MoeFee: moe,
        HdfFee: hdf,
        AddressPhys: document.getElementById('setAddressPhys').value,
        AddressPost: document.getElementById('setAddressPost').value,
        Phone: document.getElementById('setPhone').value,
        Email: document.getElementById('setEmail').value,
        BankName: document.getElementById('setBankName').value,
        AccName: document.getElementById('setAccName').value,
        AccNo: document.getElementById('setAccNo').value,
        Branch: document.getElementById('setBranch').value,
        LogoData: logoData
    };

    try {
        const response = await fetch('/api/settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(settings)
        });

        if (response.ok) {
            alert("Settings saved successfully!");
            initSettings();
        } else {
            alert("Failed to save settings.");
        }
    } catch (err) {
        alert("Error saving settings: " + err.message);
    }
}

async function activateLicense() {
    const key = document.getElementById('licenseKeyInput').value;
    if (!key) return alert("Please enter a license key.");

    try {
        const response = await fetch('/api/settings/license', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ key: key })
        });

        const result = await response.json();
        if (response.ok) {
            alert("Success: " + result.Message);
            initSettings();
        } else {
            alert("Error: " + result.Message);
        }
    } catch (err) {
        alert("License Activation Failed: " + err.message);
    }
}

function applySettings(settings) {
    if (!settings) return;

    const displayLogo = document.getElementById('hostelNameDisplay');
    const placeholder = document.getElementById('hostelLogoPlaceholder');
    const imgLogo = document.getElementById('hostelLogoImg');

    if (displayLogo) displayLogo.innerText = settings.HostelName || "HostelPro";

    if (settings.LogoData) {
        if (imgLogo) {
            imgLogo.src = settings.LogoData;
            imgLogo.style.display = 'block';
        }
        if (placeholder) placeholder.style.display = 'none';
        updateThemeFromLogo(settings.LogoData);
    }

    if (settings.HostelName) document.title = `${settings.HostelName} | Dashboard`;
}

function updateThemeFromLogo(base64) {
    if (!base64) return;
    const img = new Image();
    img.onload = function () {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        const p = ctx.getImageData(img.width / 2, img.height / 2, 1, 1).data;
        if (p[3] > 128) {
            const primary = `rgb(${p[0]}, ${p[1]}, ${p[2]})`;
            document.documentElement.style.setProperty('--primary', primary);
            if (document.getElementById('colorPreview'))
                document.getElementById('colorPreview').style.background = primary;
        }
    };
    img.src = base64;
}

document.addEventListener('DOMContentLoaded', () => {
    initSettings();
});
