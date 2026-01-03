/**
 * SETTINGS & PERSONALIZATION MODULE (Persistent Version)
 */

async function initSettings() {
    try {
        const res = await fetch('/api/settings');
        if (res.ok) {
            const settings = await res.json();
            
            // If API returns empty object (first run), fallback to defaults or localStorage if present
            if (!settings.hostelName && !settings.currency) {
                const local = localStorage.getItem('hostelSettings');
                if (local) {
                    applySettings(JSON.parse(local)); // Temp apply local
                    // Optional: Auto-save local to DB? Let's leave it to manual save.
                } else {
                    applyDefaults();
                }
                return;
            }
            
            applySettings(settings);
            populateForm(settings);
        }
    } catch (err) {
        console.error("Failed to load settings from DB", err);
    }
}

function applyDefaults() {
    if (document.getElementById('setCurrency')) {
        document.getElementById('setCurrency').value = "N$";
        document.getElementById('setFullFee').value = "2119.00";
        calculateSettingsSplit();
    }
}

function populateForm(settings) {
    if (!document.getElementById('setHostelName')) return; // Not on settings page

    document.getElementById('setHostelName').value = settings.hostelName || "";
    document.getElementById('setLogoText').value = settings.logoText || "";

    // Financials
    document.getElementById('setCurrency').value = settings.currency || "N$";
    document.getElementById('setFullFee').value = settings.fullFee || 2119;
    calculateSettingsSplit();

    // Contact
    document.getElementById('setAddressPhys').value = settings.addressPhys || "";
    document.getElementById('setAddressPost').value = settings.addressPost || "";
    document.getElementById('setPhone').value = settings.phone || "";
    document.getElementById('setEmail').value = settings.email || "";

    // Bank
    document.getElementById('setBankName').value = settings.bankName || "";
    document.getElementById('setAccName').value = settings.accName || "";
    document.getElementById('setAccNo').value = settings.accNo || "";
    document.getElementById('setBranch').value = settings.branch || "";

    if (settings.logoFileName) {
        document.getElementById('logoFileName').innerText = settings.logoFileName;
    }
    
    // Store logo data in form dataset for saving later (if not changed)
    if (settings.logoData) {
        document.getElementById('brandingForm').dataset.savedLogo = settings.logoData;
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
    const btn = event.target;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
    btn.disabled = true;

    try {
        const tempLogo = document.getElementById('brandingForm').dataset.tempLogo;
        const savedLogo = document.getElementById('brandingForm').dataset.savedLogo;
        
        const tempName = document.getElementById('brandingForm').dataset.tempLogoName;
        // If we have a saved logo but no temp logo (no new upload), keep the saved one
        // If we have a temp logo, use it.
        
        const logoData = tempLogo || savedLogo || "";
        // If uploading new, use temp name. If existing, we might not have the name easily unless we stored it.
        // Simplified: if tempName exists, use it, else ignore or keep old if backend supports partial updates (it doesn't).
        // Let's rely on the form state.
        
        const full = parseFloat(document.getElementById('setFullFee').value) || 0;
        const moe = 619.00;
        const hdf = Math.max(0, full - moe);

        const settings = {
            hostelName: document.getElementById('setHostelName').value,
            logoText: document.getElementById('setLogoText').value,
            logoData: logoData,
            logoFileName: tempName || document.getElementById('logoFileName').innerText,

            currency: document.getElementById('setCurrency').value || "N$",
            fullFee: full,
            moeFee: moe,
            hdfFee: hdf,

            addressPhys: document.getElementById('setAddressPhys').value,
            addressPost: document.getElementById('setAddressPost').value,
            phone: document.getElementById('setPhone').value,
            email: document.getElementById('setEmail').value,

            bankName: document.getElementById('setBankName').value,
            accName: document.getElementById('setAccName').value,
            accNo: document.getElementById('setAccNo').value,
            branch: document.getElementById('setBranch').value
        };

        const res = await fetch('/api/settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(settings)
        });

        if (res.ok) {
            localStorage.setItem('hostelSettings', JSON.stringify(settings)); // Keep backup
            applySettings(settings);
            alert("Settings saved to database successfully!");
        } else {
            const err = await res.json();
            alert("Error saving settings: " + err.Message);
        }
    } catch (e) {
        alert("Network error: " + e.message);
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
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
    if (!base64) return;
    const img = new Image();
    img.onload = function () {
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        // Simple pixel sampling
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

// --- SYSTEM TOOLS ---

async function runMigration() {
    if (!confirm("This will attempt to create missing columns in the database. Continue?")) return;

    const btn = event.target;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Running...';
    btn.disabled = true;

    try {
        const res = await fetch('/api/learner/fix-db'); // Updated endpoint name to match Controller
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
