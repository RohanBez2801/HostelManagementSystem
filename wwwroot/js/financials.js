/**
 * FINANCIAL & SPLIT-ACCOUNTING MODULE
 */

async function openPaymentModal() {
    const modal = document.getElementById('paymentModal');
    const select = document.getElementById('payStudentSelect');
    modal.style.display = 'flex';
    document.getElementById('payDate').valueAsDate = new Date();

    try {
        const res = await fetch('/api/learner/list-all');
        const students = await res.json();
        select.innerHTML = '<option value="">-- Choose Learner --</option>' +
            students.map(s => `<option value="${s.Id}">${s.Name} (${s.AdNo})</option>`).join('');
    } catch (err) { console.error("Error loading students:", err); }
}

function calculateSplit() {
    const total = parseFloat(document.getElementById('payAmount').value) || 0;
    const moeFixedLimit = 619.00;

    // UI-only preview of the split
    let moe = total <= moeFixedLimit ? total : moeFixedLimit;
    let hdf = total <= moeFixedLimit ? 0 : total - moeFixedLimit;

    document.getElementById('moeSplitDisplay').innerText = `N$ ${moe.toFixed(2)}`;
    document.getElementById('hdfSplitDisplay').innerText = `N$ ${hdf.toFixed(2)}`;
}

async function savePayment() {
    const payload = {
        LearnerId: parseInt(document.getElementById('payStudentSelect').value),
        TotalAmount: parseFloat(document.getElementById('payAmount').value),
        MinReceipt: document.getElementById('minReceipt').value,
        HdfReceipt: document.getElementById('hdfReceipt').value,
        PaymentDate: document.getElementById('payDate').value
    };

    if (!payload.LearnerId || !payload.TotalAmount) {
        alert("Please select a student and enter an amount.");
        return;
    }

    const res = await fetch('/api/financial/record-payment', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        alert("Payment Recorded.");
        closePaymentModal();
        loadPaymentLedger();
    }
}

function closePaymentModal() {
    document.getElementById('paymentModal').style.display = 'none';
    document.getElementById('paymentForm').reset();
}


/**
* GENERATE PRINTABLE STATEMENT
*/
async function viewStatement(learnerId, name) {
    try {
        const res = await fetch(`/api/financial/statement/${learnerId}`);
        const data = await res.json();

        let html = `
            <div id="printArea">
                <h2 style="text-align:center">Hostel Statement of Account</h2>
                <hr>
                <p><strong>Student:</strong> ${name}</p>
                <p><strong>Date:</strong> ${new Date().toLocaleDateString()}</p>
                <table style="width:100%; border-collapse: collapse; margin-top:20px;" border="1">
                    <thead>
                        <tr style="background:#f1f5f9">
                            <th>Date</th>
                            <th>Receipts (MoE/HDF)</th>
                            <th>MoE Fund</th>
                            <th>HDF Fund</th>
                            <th>Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.map(line => `
                            <tr>
                                <td>${line.Date}</td>
                                <td>${line.Receipts}</td>
                                <td>N$ ${line.MoE.toFixed(2)}</td>
                                <td>N$ ${line.HDF.toFixed(2)}</td>
                                <td style="font-weight:bold">N$ ${line.Total.toFixed(2)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
            <button onclick="window.print()" class="btn-submit" style="margin-top:20px">Print Statement</button>
        `;

        // You can inject this into a generic modal or a new window
        openModal(html);
    } catch (err) {
        alert("Could not generate statement.");
    }
}