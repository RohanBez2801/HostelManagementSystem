// COMMUNICATION MODULE

async function loadCommunicationLog() {
    const tbody = document.getElementById('commTableBody');
    if (!tbody) return;

    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;">Loading history...</td></tr>';

    try {
        const res = await fetch('/api/Communication/log');
        const data = await res.json();

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center; color:#94a3b8;">No communication history found.</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(d => `
            <tr>
                <td>${d.date}</td>
                <td><span class="badge" style="background:${d.type === 'Email' ? '#dbeafe' : '#fce7f3'}; color:${d.type === 'Email' ? '#1e40af' : '#9d174d'}">${d.type}</span></td>
                <td>${d.recipient}</td>
                <td><strong>${d.subject}</strong></td>
                <td>${d.status}</td>
            </tr>
        `).join('');
    } catch (err) { tbody.innerHTML = '<tr><td colspan="5" style="color:red; text-align:center;">Error loading history.</td></tr>'; }
}

async function sendEmail() {
    const type = document.getElementById('commRecipType').value;
    const val = document.getElementById('commRecipValue').value;
    const subject = document.getElementById('commSubject').value;
    const body = document.getElementById('commBody').value;

    if (!subject || !body) return alert("Subject and Body are required.");

    const btn = document.getElementById('btnSendEmail');
    const oldText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';
    btn.disabled = true;

    try {
        await fetch('/api/Communication/email/send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                RecipientType: type,
                RecipientValue: val,
                Subject: subject,
                Body: body
            })
        });

        alert("Email(s) sent successfully.");
        document.getElementById('emailForm').reset();
        loadCommunicationLog();
    } catch (e) {
        alert("Failed to send email.");
    } finally {
        btn.innerHTML = oldText;
        btn.disabled = false;
    }
}

function handleRecipChange() {
    const type = document.getElementById('commRecipType').value;
    const valContainer = document.getElementById('commRecipValContainer');
    
    if (type === 'Grade') {
        valContainer.style.display = 'block';
        valContainer.innerHTML = `<label>Select Grade</label><select id="commRecipValue" style="width:100%"><option value="8">Grade 8</option><option value="9">Grade 9</option><option value="10">Grade 10</option><option value="11">Grade 11</option><option value="12">Grade 12</option></select>`;
    } else if (type === 'Single') {
        valContainer.style.display = 'block';
        valContainer.innerHTML = `<label>Enter Parent Email or Name</label><input type="text" id="commRecipValue" placeholder="Search parent..." style="width:100%">`;
    } else {
        valContainer.style.display = 'none';
        valContainer.innerHTML = `<input type="hidden" id="commRecipValue" value="">`;
    }
}
