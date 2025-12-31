async function loadDiningLog() {
    const tbody = document.getElementById('diningTableBody');
    if (!tbody) return;

    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;">Loading records...</td></tr>';

    try {
        const res = await fetch('/api/Dining/log');
        const data = await res.json();

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center; color:#94a3b8;">No supply logs found.</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(d => `
            <tr>
                <td>${d.date}</td>
                <td><strong>${d.supplier}</strong></td>
                <td>${d.item}</td>
                <td>${d.quantity}</td>
                <td><span class="badge" style="background:#e0f2fe; color:#0284c7;">${d.receivedBy}</span></td>
            </tr>
        `).join('');
    } catch (err) { tbody.innerHTML = '<tr><td colspan="5" style="color:red; text-align:center;">Error loading logs.</td></tr>'; }
}

async function saveDiningLog() {
    const supplier = document.getElementById('dinSupplier').value;
    const item = document.getElementById('dinItem').value;
    const qty = document.getElementById('dinQty').value;
    const receiver = sessionStorage.getItem('userName') || "Unknown";

    if (!supplier || !item || !qty) return alert("Please fill all fields");

    const btn = document.querySelector('#diningModal .btn-submit');
    const oldText = btn.innerText;
    btn.innerText = "Saving...";
    btn.disabled = true;

    try {
        await fetch('/api/Dining/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Supplier: supplier, Item: item, Quantity: qty, ReceivedBy: receiver })
        });

        document.getElementById('diningForm').reset();
        document.getElementById('diningModal').style.display = 'none';
        loadDiningLog();
    } catch (e) {
        alert("Failed to save log");
    } finally {
        btn.innerText = oldText;
        btn.disabled = false;
    }
}
