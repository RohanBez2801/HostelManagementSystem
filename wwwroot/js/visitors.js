// js/visitors.js

function loadVisitors() {
    fetch('/api/visitor/all')
        .then(res => res.json())
        .then(data => {
            const tbody = document.getElementById('visitorTableBody');
            tbody.innerHTML = '';
            data.forEach(v => {
                const isOut = v.timeOut !== '-';
                const row = `<tr>
                    <td>${v.date}</td>
                    <td>${v.name}</td>
                    <td>${v.phone}</td>
                    <td>${v.student}</td>
                    <td>${v.timeIn}</td>
                    <td>${v.timeOut}</td>
                    <td>
                        ${!isOut ? `<button class="btn-sm btn-primary" onclick="checkoutVisitor(${v.id})">Check Out</button>` : '<span class="text-muted">Completed</span>'}
                    </td>
                </tr>`;
                tbody.innerHTML += row;
            });
        })
        .catch(err => console.error(err));
}

function openCheckInModal() {
    document.getElementById('checkInModal').style.display = 'flex';
    // Load students
    fetch('/api/learner/all')
        .then(res => res.json())
        .then(data => {
            const sel = document.getElementById('visStudentSelect');
            sel.innerHTML = '<option value="">-- Select Student --</option>';
            data.forEach(s => {
                sel.innerHTML += `<option value="${s.id}">${s.surname} ${s.names} (Grade ${s.grade})</option>`;
            });
        });
}

function closeCheckInModal() {
    document.getElementById('checkInModal').style.display = 'none';
}

function checkInVisitor() {
    const visitor = {
        visitorName: document.getElementById('visName').value,
        phone: document.getElementById('visPhone').value,
        learnerID: parseInt(document.getElementById('visStudentSelect').value)
    };

    fetch('/api/visitor/checkin', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(visitor)
    })
    .then(res => {
        if (res.ok) {
            closeCheckInModal();
            loadVisitors();
            alert('Visitor checked in!');
        } else {
            alert('Error checking in visitor');
        }
    });
}

function checkoutVisitor(id) {
    if(!confirm('Check out this visitor?')) return;
    fetch(`/api/visitor/checkout/${id}`, { method: 'PUT' })
        .then(res => {
            if(res.ok) loadVisitors();
        });
}
