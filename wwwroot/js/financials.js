/**
 * FINANCIALS MODULE
 * Handles Cash Book, Split Payments, Expenses, and Imports
 */

// --- 1. LOAD THE UNIFIED LEDGER ---
async function loadPaymentLedger() {
    const tbody = document.getElementById('paymentListBody');
    if (!tbody) return;

    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;">Loading Cash Book...</td></tr>';
    const currency = typeof getCurrency === 'function' ? getCurrency() : "N$";

    try {
        const res = await fetch('/api/Financials/cashbook');
        const transactions = await res.json();

        if (!res.ok) throw new Error("Failed to load data");

        if (transactions.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center;">No transactions found.</td></tr>';
            return;
        }

        tbody.innerHTML = transactions.map(t => {
            const isIncome = t.type === 'Income';
            const color = isIncome ? '#16a34a' : '#dc2626';
            const badgeBg = isIncome ? '#dcfce7' : '#fee2e2';

            // Fix: Ensure we read the correct property (t.description)
            const desc = t.description || t.Description || "Unknown";

            return `
                <tr>
                    <td style="color:#64748b; font-size:13px;">${t.date}</td>
                    <td style="font-weight:600; color:#334155;">${desc}</td>
                    <td><span style="background:${badgeBg}; color:${color}; padding:4px 8px; border-radius:6px; font-size:11px; font-weight:700;">${t.vote || t.type}</span></td>
                    <td style="font-weight:700; color:${color}; text-align:right;">${currency} ${t.amount.toFixed(2)}</td>
                    <td style="font-size:12px; color:#64748b;">${t.refNo || '-'}</td>
                    <td>
                        <button class="btn-icon" style="color:#94a3b8" title="View"><i class="fas fa-eye"></i></button>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="color:red; text-align:center;">Error: ${err.message}</td></tr>`;
    }
}

// --- 2. IMPORT REVENUE FROM CSV (NEW FEATURE) ---
function openRevenueImport() {
    // Create a hidden file input dynamically
    let input = document.createElement('input');
    input.type = 'file';
    input.accept = '.csv';
    input.onchange = e => {
        const file = e.target.files[0];
        if (file) processRevenueCSV(file);
    };
    input.click();
}

function processRevenueCSV(file) {
    const reader = new FileReader();
    reader.onload = async function (e) {
        const text = e.target.result;
        // Basic CSV Parsing (Splitting by line and comma)
        const rows = text.split('\n');
        const payload = [];

        // We assume Row 5 (index 4) onwards is data based on your file structure
        for (let i = 4; i < rows.length; i++) {
            // Handle commas inside quotes if necessary, but for now simple split:
            const cols = rows[i].split(',');

            if (cols.length < 7 || !cols[0]) continue; // Skip empty rows

            // MAPPING based on 'REVENUE (Income) CashBook.csv'
            // Col A (0) = Date
            // Col B (1) = HDF Receipt
            // Col D (3) = Recipient Name (Payee)
            // Col F (5) = HDF Amount (A: LEARNERS' HOSTEL FEES)
            // Col G (6) = MoE Amount (B: LEARNERS' GOVERNMENT...)

            const dateStr = cols[0].trim();
            const receipt = cols[1].trim();
            const payer = cols[3].trim().replace(/"/g, ''); // Remove quotes if any

            // Parse Numbers (Remove any non-numeric chars except dot)
            const amountHDF = parseFloat(cols[5]) || 0;
            const amountMoE = parseFloat(cols[6]) || 0;

            if (dateStr && (amountHDF > 0 || amountMoE > 0)) {
                // Try to format date to ISO (YYYY-MM-DD)
                // Note: Ensure date string is parseable by JS (e.g. 2025-01-02)
                let isoDate = new Date(dateStr).toISOString();

                payload.push({
                    Date: isoDate,
                    ReceiptNo: receipt,
                    Payee: payer,
                    AmountHDF: amountHDF,
                    AmountMoE: amountMoE
                });
            }
        }

        if (payload.length === 0) {
            alert("No valid transactions found in CSV. Please check the format.");
            return;
        }

        if (!confirm(`Found ${payload.length} transactions. Import them now?`)) return;

        // Send to Server
        try {
            const res = await fetch('/api/Financials/import/revenue', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await res.json();
            if (res.ok) {
                alert(result.Message);
                loadPaymentLedger(); // Refresh table
                if (typeof updateStats === 'function') updateStats(); // Refresh dashboard stats
            } else {
                alert("Import Failed: " + result.Message);
            }
        } catch (err) {
            alert("Network Error: " + err.message);
        }
    };
    reader.readAsText(file);
}

// --- 3. MODAL LOGIC (Existing) ---

function openPaymentModal() {
    document.getElementById('paymentModal').style.display = 'flex';
}

function closePaymentModal() {
    document.getElementById('paymentModal').style.display = 'none';
    // Optional: Clear form
}

function openExpenseModal() {
    document.getElementById('expenseModal').style.display = 'flex';

    // Load Votes dynamically if empty
    const select = document.getElementById('expVote');
    if (select.options.length <= 1) {
        fetch('/api/Financials/votes')
            .then(r => r.json())
            .then(data => {
                // Populate dropdown
                select.innerHTML = '<option value="">-- Select Vote --</option>' +
                    data.map(v => `<option value="${v.id}">${v.name}</option>`).join('');
            })
            .catch(err => console.error("Failed to load votes", err));
    }
}

function closeExpenseModal() {
    document.getElementById('expenseModal').style.display = 'none';
}

// --- 4. SAVE FUNCTIONS (Existing) ---

async function saveIncome() {
    const data = {
        AdmissionNo: document.getElementById('payStudentId').value,
        Amount: parseFloat(document.getElementById('payAmount').value),
        VoteId: parseInt(document.getElementById('payVote').value),
        Reference: document.getElementById('payRef').value
    };

    if (!data.AdmissionNo || !data.Amount) {
        alert("Please fill in Student ID and Amount");
        return;
    }

    try {
        const res = await fetch('/api/Financials/pay', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (res.ok) {
            alert("Payment Saved!");
            closePaymentModal();
            loadPaymentLedger();
            // Clear Form
            document.getElementById('payStudentId').value = '';
            document.getElementById('payAmount').value = '';
            document.getElementById('payRef').value = '';
        } else {
            const err = await res.json();
            alert("Error: " + err.Message);
        }
    } catch (err) {
        alert("Network Error: " + err.message);
    }
}

async function saveExpense() {
    const data = {
        Payee: document.getElementById('expPayee').value,
        Amount: parseFloat(document.getElementById('expAmount').value),
        VoteId: parseInt(document.getElementById('expVote').value),
        Reference: document.getElementById('expRef').value
    };

    if (!data.Payee || !data.Amount || !data.VoteId) {
        alert("Please fill in Payee, Amount and select a Vote.");
        return;
    }

    try {
        const res = await fetch('/api/Financials/expense', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (res.ok) {
            alert("Expenditure Recorded!");
            closeExpenseModal();
            loadPaymentLedger();

            // Clear Form
            document.getElementById('expPayee').value = '';
            document.getElementById('expAmount').value = '';
            document.getElementById('expRef').value = '';
        } else {
            const err = await res.json();
            alert("Error: " + err.Message);
        }
    } catch (err) {
        alert("Network Error: " + err.message);
    }
}