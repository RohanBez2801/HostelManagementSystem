/**
 * ROOM & OCCUPANCY MODULE
 */

async function loadRoomMapping() {
    const container = document.getElementById('roomGridContainer');
    if (!container) return;

    try {
        const res = await fetch('/api/Room/all');
        const rooms = await res.json();

        container.innerHTML = rooms.map(room => {
            const isFull = room.occupied >= room.capacity;
            const percentage = (room.occupied / room.capacity) * 100;

            return `
                <div class="room-card ${isFull ? 'room-full' : ''}">
                    <div class="flex-between">
                        <h4>Room ${room.roomNumber}</h4>
                        <span class="badge">${room.occupied}/${room.capacity}</span>
                    </div>
                    <div class="progress-bar">
                        <div class="progress-fill" style="width: ${percentage}%"></div>
                    </div>
                    <p style="font-size:12px; margin-top:5px; color:#64748b">
                        ${isFull ? 'No Vacancy' : (room.capacity - room.occupied) + ' Beds Available'}
                    </p>
                    <button class="btn-sm" onclick="viewRoomDetails(${room.id})">View Occupants</button>
                </div>
            `;
        }).join('');
    } catch (err) {
        console.error("Error loading rooms:", err);
    }
}

// Utility to populate Room Dropdowns in Registration form
async function loadRoomsForSelect() {
    const select = document.getElementById('regRoomSelect');
    if (!select) return;

    try {
        const res = await fetch('/api/Room/all');
        const rooms = await res.json();

        // Only show rooms with space
        select.innerHTML = '<option value="">-- Select Room --</option>' +
            rooms.filter(r => r.occupied < r.capacity)
                .map(r => `<option value="${r.id}">Room ${r.roomNumber} (${r.capacity - r.occupied} left)</option>`)
                .join('');
    } catch (err) {
        console.error("Select error:", err);
    }
}