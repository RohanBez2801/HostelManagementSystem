// js/inventory.js

function loadInventory() {
    fetch('/api/inventory/all')
        .then(res => res.json())
        .then(data => {
            const tbody = document.getElementById('inventoryTableBody');
            tbody.innerHTML = '';
            data.forEach(item => {
                const row = `<tr>
                    <td>${item.name}</td>
                    <td>${item.category}</td>
                    <td>${item.quantity}</td>
                    <td><span class="badge ${item.condition === 'Good' ? 'badge-success' : 'badge-warning'}">${item.condition}</span></td>
                    <td>${item.room}</td>
                    <td>
                        <button class="btn-sm btn-danger" onclick="deleteInventory(${item.id})"><i class="fas fa-trash"></i></button>
                    </td>
                </tr>`;
                tbody.innerHTML += row;
            });
        })
        .catch(err => console.error(err));
}

function openAddInventoryModal() {
    document.getElementById('addInventoryModal').style.display = 'flex';
    // Load rooms for dropdown
    fetch('/api/room/all')
        .then(res => res.json())
        .then(data => {
            const sel = document.getElementById('invRoomSelect');
            sel.innerHTML = '<option value="0">General Store</option>';
            data.forEach(r => {
                sel.innerHTML += `<option value="${r.id}">${r.number} (${r.block})</option>`;
            });
        });
}

function closeAddInventoryModal() {
    document.getElementById('addInventoryModal').style.display = 'none';
}

function saveInventory() {
    const item = {
        name: document.getElementById('invName').value,
        category: document.getElementById('invCategory').value,
        quantity: parseInt(document.getElementById('invQuantity').value),
        condition: document.getElementById('invCondition').value,
        roomId: parseInt(document.getElementById('invRoomSelect').value)
    };

    fetch('/api/inventory/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(item)
    })
    .then(res => {
        if (res.ok) {
            closeAddInventoryModal();
            loadInventory();
            alert('Item added!');
        } else {
            alert('Error adding item');
        }
    });
}

function deleteInventory(id) {
    if(!confirm('Delete this item?')) return;
    fetch(`/api/inventory/delete/${id}`, { method: 'DELETE' })
        .then(res => {
            if(res.ok) loadInventory();
        });
}
