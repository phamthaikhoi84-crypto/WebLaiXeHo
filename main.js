const API_URL = "http://localhost:5173/api";
const HUB_URL = "http://localhost:5173/hubs/ride";
let map, connection, gpsWatchId, driverMarker, pickupMarker, destMarker;
let pickupCoords = null, destCoords = null, activeRideId = null, searchTimer;

let currentRole = null, currentEmail = null, selectedRating = 0;
let currentRatingRideId = null; 
let lastLoadedChatRideId = null;

const swalOpts = { background: '#fff', color: '#1E293B', confirmButtonColor: '#1A6B3D', borderRadius: '16px' };

function safeClassList(id, action, className) {
    const el = document.getElementById(id);
    if (el) { el.classList[action](className); }
}

async function getAddressFromCoords(lng, lat) {
    try {
        const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
        const data = await res.json();
        return data.display_name.split(',').slice(0, 2).join(',').trim();
    } catch (e) { return `${lat.toFixed(4)}, ${lng.toFixed(4)}`; }
}

function getCurrentLocation(isManual = true) {
    if (!navigator.geolocation) {
        if (isManual) Swal.fire({...swalOpts, icon: 'error', text: 'Trình duyệt không hỗ trợ GPS'});
        return;
    }
    if (isManual) Swal.fire({ title: 'Đang định vị...', text: 'Vui lòng chờ', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); }});
    
    navigator.geolocation.getCurrentPosition(async (pos) => {
        const coords = [pos.coords.longitude, pos.coords.latitude];
        const address = await getAddressFromCoords(coords[0], coords[1]);
        setMarker(coords, 'pickup', address, true);
        map.flyTo({ center: coords, zoom: 16 });
        if (isManual) Swal.close();
    }, (err) => {
        if (isManual) Swal.fire({...swalOpts, icon: 'error', text: 'Không thể lấy vị trí. Hãy bật GPS!'});
    }, { enableHighAccuracy: true });
}

function goHome() {
    safeClassList('view-profile', 'add', 'hidden');
    safeClassList('view-admin', 'add', 'hidden'); // ✅ Thêm dòng này
    safeClassList('app-container', 'remove', 'hidden');
    
    const token = localStorage.getItem('lmd_token');
    if (!token) {
        safeClassList('view-customer', 'remove', 'hidden');
        safeClassList('loginOverlay', 'remove', 'hidden');
        initMap();
    } else {
        safeClassList('loginOverlay', 'add', 'hidden');
        
        // ✅ THÊM LOGIC RẼ NHÁNH CHO ADMIN
        if (currentRole === 'Admin') {
            safeClassList('app-container', 'add', 'hidden'); // Ẩn cả app chính đi
            safeClassList('view-admin', 'remove', 'hidden'); // Bật màn hình Admin lên
            loadAdminDashboard(); 
        } 
        else if(currentRole === 'Driver') {
            safeClassList('view-customer', 'add', 'hidden');
            safeClassList('view-driver', 'remove', 'hidden');
            safeClassList('btnMyLocation', 'add', 'hidden'); 
            
            // ✅ ĐỊNH VỊ TÀI XẾ LIÊN TỤC ĐỂ THUẬT TOÁN ĐO KHOẢNG CÁCH HOẠT ĐỘNG
            if (!window.idleGpsWatch && navigator.geolocation) {
                window.idleGpsWatch = navigator.geolocation.watchPosition(pos => {
                    window.driverCurrentCoords = [pos.coords.longitude, pos.coords.latitude];
                }, null, { enableHighAccuracy: true });
            }
            fetchPendingRides();
        } else {
            safeClassList('view-driver', 'add', 'hidden');
            safeClassList('view-customer', 'remove', 'hidden');
            safeClassList('btnMyLocation', 'remove', 'hidden');
        }
        
        if(currentRole !== 'Admin') checkCurrentRide(); // Admin ko cần quét cuốc xe
    }
    if(map) setTimeout(() => map.resize(), 300);
}

function showProfile() {
    safeClassList('app-container', 'add', 'hidden');
    safeClassList('view-profile', 'remove', 'hidden');
    fetchHistory();
}

async function checkAuthState() {
    const token = localStorage.getItem('lmd_token'); 
    const nav = document.getElementById('top-auth-actions');
    if (!nav) return;

    if (!token) {
        currentRole = null; currentEmail = null;
        nav.innerHTML = `<button onclick="showModal('loginModal')" class="bg-thuelai text-white px-5 py-2 rounded-lg font-bold text-xs hover:bg-thuelai-hover transition shadow-sm">Đăng nhập</button>`;
        goHome();
    } else {
        try {
            const p = JSON.parse(atob(token.split('.')[1]));
            currentRole = p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || p.role;
            currentEmail = p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || p.email;
            
            // ✅ KÉO HỒ SƠ TỪ DATABASE (Gồm Tên, SĐT, Avatar)
            let myProfile = { Name: currentEmail.split('@')[0].toUpperCase(), Phone: '---', Avatar: null };
            try {
                const profRes = await fetch(`${API_URL}/Auth/profile`, { headers: { 'Authorization': `Bearer ${token}` } });
                if(profRes.ok) {
                    const pData = await profRes.json();
                    myProfile = { Name: pData.name || pData.Name, Phone: pData.phone || pData.Phone, Avatar: pData.avatar || pData.Avatar };
                }
            } catch(e){}

            document.getElementById('dispEmail').innerText = currentEmail;
            document.getElementById('dispName').innerText = myProfile.Name;
            document.getElementById('dispPhone').innerText = myProfile.Phone;
            
            const shortName = myProfile.Name;
            const savedAvatar = myProfile.Avatar; 
            
            let balance = 0;
            try {
                const balRes = await fetch(`${API_URL}/Wallet/balance`, { headers: { 'Authorization': `Bearer ${token}` } });
                if(balRes.ok) { const bData = await balRes.json(); balance = bData.Balance || bData.balance || 0; }
            } catch(e){}
            
            nav.innerHTML = `
                <div class="relative">
                    <button onclick="toggleAccountMenu(event)" id="avatarBtn" class="flex items-center gap-2 bg-gray-100 p-1 pr-3 rounded-full hover:bg-gray-200 transition shadow-sm">
                        <div id="headerAvatarImage" class="w-8 h-8 rounded-full bg-thuelai text-white flex items-center justify-center text-sm font-bold border border-white overflow-hidden">
                            ${savedAvatar ? `<img src="${savedAvatar}" class="w-full h-full object-cover">` : shortName.charAt(0).toUpperCase()}
                        </div>
                        <span class="text-[10px] font-bold text-gray-700 hidden sm:block uppercase">${shortName}</span>
                        <i class="fas fa-chevron-down text-[10px] text-gray-400"></i>
                    </button>
                    <div id="accountDropdown" class="hidden absolute top-full right-0 mt-3 w-48 bg-white rounded-2xl shadow-2xl border border-gray-100 overflow-hidden z-[100]">
                        <div class="p-2">
                            <button onclick="showProfile()" class="w-full text-left p-3 text-xs font-bold text-gray-600 hover:bg-green-50 rounded-xl transition">Xem hồ sơ</button>
                            <button onclick="logout()" class="w-full text-left p-3 text-xs font-bold text-red-500 hover:bg-red-50 rounded-xl transition">Đăng xuất</button>
                        </div>
                    </div>
                </div>`;
            
            const displayProfileAvatar = document.getElementById('profileAvatarDisplay');
            if (displayProfileAvatar) {
                displayProfileAvatar.innerHTML = savedAvatar ? `<img src="${savedAvatar}" class="w-full h-full object-cover">` : `<i class="fas fa-user-circle"></i>`;
            }
            
            initMap(); connectSignalR(token, currentRole); goHome();
        } catch(e) { logout(); }
    }
}

async function processLogin() {
    const btn = document.getElementById('btnLogin');
    btn.disabled = true; btn.innerHTML = '<i class="fas fa-circle-notch fa-spin"></i>';
    try {
        const res = await fetch(`${API_URL}/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: document.getElementById('loginEmail').value, password: document.getElementById('loginPass').value }) });
        if (res.ok) { 
            const data = await res.json(); 
            localStorage.setItem('lmd_token', data.Token || data.token); 
            hideModal('loginModal'); checkAuthState(); 
        } else { 
            // ✅ HIỂN THỊ CHÍNH XÁC LỖI TỪ BACKEND (Bị khóa, sai pass...)
            const errText = await res.text();
            Swal.fire({...swalOpts, icon: 'error', text: errText || 'Đăng nhập thất bại.'}); 
        }
    } catch (e) { Swal.fire({...swalOpts, icon: 'error', text: 'Mất kết nối server.'}); }
    finally { btn.disabled = false; btn.innerText = 'VÀO HỆ THỐNG'; }
}

async function processRegister() {
    let licenseImgBase64 = null;
    let avatarBase64 = null;
    
    const role = document.getElementById('regRole').value;
    const name = document.getElementById('regName').value.trim();
    const phone = document.getElementById('regPhone').value.trim();
    const email = document.getElementById('regEmail').value.trim();
    const pass = document.getElementById('regPass').value;

    // Ràng buộc nhập liệu cơ bản
    if(!name || !email || !pass) {
        return Swal.fire({...swalOpts, icon: 'warning', text: 'Vui lòng nhập đủ Họ tên, Email và Mật khẩu!'});
    }

    // Lấy ảnh đại diện
    const avatarPreview = document.getElementById('regAvatarPreview');
    if (avatarPreview.src && avatarPreview.src.startsWith('data:image')) {
        avatarBase64 = avatarPreview.src;
    }

    // Ràng buộc Tài xế phải up ảnh bằng lái
    if (role === 'Driver') {
        const licensePreview = document.getElementById('regLicensePreview');
        if (!licensePreview.src || !licensePreview.src.startsWith('data:image')) {
            return Swal.fire({...swalOpts, icon: 'warning', text: 'Vui lòng tải lên ảnh chụp Bằng lái xe của bạn!'});
        }
        licenseImgBase64 = licensePreview.src;
    }

    const payload = { 
        name: name,
        phone: phone,
        email: email, 
        password: pass, 
        role: role, 
        avatar: avatarBase64,
        licenseType: role === 'Driver' ? parseInt(document.getElementById('regLicense').value) : null,
        licenseImage: licenseImgBase64 
    };
    
    Swal.fire({ title: 'Đang xử lý...', didOpen: () => Swal.showLoading() });
    
    try {
        const res = await fetch(`${API_URL}/auth/register`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
        if (res.ok) {
            Swal.fire({...swalOpts, icon: 'success', text: 'Đăng ký thành công!'}).then(() => { 
                hideModal('registerModal'); showModal('loginModal'); 
            });
        }
        else {
            Swal.fire({...swalOpts, icon: 'error', text: await res.text()});
        }
    } catch (e) { 
        Swal.fire({...swalOpts, icon: 'error', text: 'Mất kết nối mạng.'}); 
    }
}

let isEditMode = false;
function toggleEditMode() {
    isEditMode = !isEditMode;
    safeClassList('profileDisplayMode', isEditMode ? 'add' : 'remove', 'hidden');
    safeClassList('profileEditMode', isEditMode ? 'remove' : 'add', 'hidden');
    const btn = document.getElementById('btnEditProfile');
    if(btn) btn.innerText = isEditMode ? 'Hủy bỏ' : 'Chỉnh sửa';
    if(isEditMode) {
        document.getElementById('editName').value = document.getElementById('dispName').innerText;
        document.getElementById('editPhone').value = document.getElementById('dispPhone').innerText === '---' ? '' : document.getElementById('dispPhone').innerText;
        document.getElementById('editAvatarInput').value = '';
        document.getElementById('editAvatarPreview').src = '';
        document.getElementById('editAvatarPreview').classList.add('hidden');
        document.getElementById('editAvatarIcon').classList.remove('hidden');
    }
}

function previewAvatar(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            document.getElementById('editAvatarPreview').src = e.target.result;
            document.getElementById('editAvatarPreview').classList.remove('hidden');
            document.getElementById('editAvatarIcon').classList.add('hidden');
        }
        reader.readAsDataURL(file);
    }
}
window.previewLicense = function(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const preview = document.getElementById('regLicensePreview');
            preview.src = e.target.result;
            preview.classList.remove('hidden');
            document.getElementById('regLicenseIcon').classList.add('hidden');
        }
        reader.readAsDataURL(file);
    }
}
window.previewRegAvatar = function(event) {
    const file = event.target.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const preview = document.getElementById('regAvatarPreview');
            preview.src = e.target.result;
            preview.classList.remove('hidden');
            document.getElementById('regAvatarIcon').classList.add('hidden');
        }
        reader.readAsDataURL(file);
    }
}

function updateAvatarDisplay(base64Image) {
    const displayAvatar = document.getElementById('profileAvatarDisplay');
    if (displayAvatar && base64Image) { displayAvatar.innerHTML = `<img src="${base64Image}" class="w-full h-full object-cover">`; }
    const headerAvatar = document.getElementById('headerAvatarImage');
    if (headerAvatar && base64Image) { headerAvatar.innerHTML = `<img src="${base64Image}" class="w-full h-full object-cover">`; }
}

async function saveProfileChanges() {
    const newName = document.getElementById('editName').value.trim();
    const newPhone = document.getElementById('editPhone').value.trim();
    const avatarPreview = document.getElementById('editAvatarPreview').src;
    
    if (!newName) return Swal.fire({...swalOpts, icon: 'warning', text: 'Vui lòng nhập tên hiển thị'});
    
    // Lấy Base64 nếu có chọn ảnh mới
    let avatarBase64 = null;
    if (avatarPreview && avatarPreview.startsWith('data:image')) {
        avatarBase64 = avatarPreview;
    }

    Swal.fire({ title: 'Đang lưu...', didOpen: () => Swal.showLoading() });
    
    try {
        const res = await fetch(`${API_URL}/Auth/update-profile`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` },
            body: JSON.stringify({ name: newName, phone: newPhone, avatar: avatarBase64 || "" })
        });

        if (res.ok) {
            Swal.fire({ ...swalOpts, icon: 'success', title: 'Đã cập nhật', timer: 1500, showConfirmButton: false });
            toggleEditMode();
            checkAuthState(); // Gọi lại hàm này để load ảnh mới nhất từ DB ra giao diện
        } else {
            Swal.fire({...swalOpts, icon: 'error', text: 'Lỗi cập nhật hồ sơ.'});
        }
    } catch(e) {
        Swal.fire({...swalOpts, icon: 'error', text: 'Mất kết nối mạng.'});
    }
}

function initMap() {
    if(map) return; 
    map = new maplibregl.Map({
        container: 'map',
        style: {
            'version': 8,
            'sources': { 'raster-tiles': { 'type': 'raster', 'tiles': ['https://a.basemaps.cartocdn.com/light_all/{z}/{x}/{y}@2x.png'], 'tileSize': 256 } },
            'layers': [{ 'id': 'simple-tiles', 'type': 'raster', 'source': 'raster-tiles', 'minzoom': 0, 'maxzoom': 22 }]
        },
        center: [106.660172, 10.762622], zoom: 14, attributionControl: false
    });

    map.on('load', () => {
        if (currentRole === 'User' && !pickupCoords && !activeRideId) { getCurrentLocation(false); }
    });

    map.on('click', async (e) => {
        if (activeRideId || currentRole === 'Driver' || !localStorage.getItem('lmd_token')) return; 
        const coords = [e.lngLat.lng, e.lngLat.lat];
        const targetId = !pickupCoords ? 'txtPickup' : (!destCoords ? 'txtDestination' : 'txtPickup');
        const input = document.getElementById(targetId);
        if(input) input.value = "Đang xác định địa chỉ...";

        const address = await getAddressFromCoords(coords[0], coords[1]);

        if (!pickupCoords) setMarker(coords, 'pickup', address, true);
        else if (!destCoords) setMarker(coords, 'dest', address, true);
        else {
            pickupCoords = coords; destCoords = null;
            if (pickupMarker) pickupMarker.remove(); if (destMarker) destMarker.remove();
            if (map.getSource('route')) { map.removeLayer('route-line'); map.removeSource('route'); }
            setMarker(coords, 'pickup', address, true);
            document.getElementById('txtDestination').value = '';
        }
    });
}

function setMarker(coords, type, addressName, updateInput = false) {
    const el = document.createElement('div'); el.className = `pin-${type}`;
    if(type === 'pickup') {
        pickupCoords = coords; if (pickupMarker) pickupMarker.remove();
        pickupMarker = new maplibregl.Marker({ element: el }).setLngLat(coords).addTo(map);
        if(updateInput) document.getElementById('txtPickup').value = addressName;
    } else {
        destCoords = coords; if (destMarker) destMarker.remove();
        destMarker = new maplibregl.Marker({ element: el }).setLngLat(coords).addTo(map);
        if(updateInput) document.getElementById('txtDestination').value = addressName;
    }
    map.flyTo({ center: coords, zoom: 15 });
    if(pickupCoords && destCoords) drawRoute(pickupCoords, destCoords);
}

async function drawRoute(start, end) {
    try {
        const res = await fetch(`https://router.project-osrm.org/route/v1/driving/${start[0]},${start[1]};${end[0]},${end[1]}?overview=full&geometries=geojson`);
        const data = await res.json();
        const distanceKm = (data.routes[0].distance / 1000).toFixed(1);
        document.getElementById('displayDist').innerText = distanceKm + ' km';
        document.getElementById('priceValue').innerText = (distanceKm * 15000).toLocaleString();
        
        if (map.getSource('route')) { map.getSource('route').setData(data.routes[0].geometry); }
        else {
            map.addSource('route', { type: 'geojson', data: data.routes[0].geometry });
            map.addLayer({ id: 'route-line', type: 'line', source: 'route', layout: { 'line-join': 'round', 'line-cap': 'round' }, paint: { 'line-color': '#1A6B3D', 'line-width': 6 } }); 
        }
        map.fitBounds(new maplibregl.LngLatBounds().extend(start).extend(end), { padding: 40 }); 
    } catch (e) { }
}

async function restoreRoute(pStr, dStr) {
    if(!map) return;
    try {
        const pRes = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(pStr)}&limit=1`);
        const pData = await pRes.json();
        await new Promise(r => setTimeout(r, 800));
        const dRes = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(dStr)}&limit=1`);
        const dData = await dRes.json();
        if(pData.length > 0 && dData.length > 0) {
            setMarker([parseFloat(pData[0].lon), parseFloat(pData[0].lat)], 'pickup', pStr, false);
            setMarker([parseFloat(dData[0].lon), parseFloat(dData[0].lat)], 'dest', dStr, false);
        }
    } catch(e){}
}

async function connectSignalR(token, role) {
    if(connection) return; 
    connection = new signalR.HubConnectionBuilder().withUrl(HUB_URL, { accessTokenFactory: () => token }).withAutomaticReconnect().build();
    
    connection.on("RideStatusUpdated", (rideId, status) => { 
        if (!activeRideId || activeRideId.toLowerCase() !== rideId.toLowerCase()) return;

        if (status === "Accepted") { checkCurrentRide(); } 
        else if (status === "Completed") { 
            activeRideId = null;
            Swal.fire({...swalOpts, icon: 'success', title: 'Hoàn tất', text: 'Chuyến đi đã kết thúc thành công!'}).then(() => location.reload()); 
        } 
        else if (status === "Cancelled") { 
            activeRideId = null;
            Swal.fire({...swalOpts, icon: 'info', text: 'Chuyến đi đã bị hủy.'}).then(() => location.reload()); 
        }
    });
    
    connection.on("LocationUpdated", (lat, lng) => {
        if (role === 'User' && map) {
            if (!driverMarker) {
                const el = document.createElement('div'); el.className = 'marker-car'; el.innerHTML = '<i class="fas fa-car"></i>';
                driverMarker = new maplibregl.Marker(el).setLngLat([lng, lat]).addTo(map);
            } else driverMarker.setLngLat([lng, lat]);
            map.panTo([lng, lat], { duration: 1500 });
        }
    });

    connection.on("ReceiveMessage", (rideId, senderRole, message) => {
        if (!activeRideId || activeRideId.toLowerCase() !== rideId.toLowerCase()) return;
        
        // Nhận tín hiệu Cuộc gọi
        if (message === "[CALL_REQUEST]" && senderRole !== currentRole) { showIncomingCall(); return; }
        if (message === "[CALL_ENDED]" && senderRole !== currentRole) { hideCallModal(); return; }
        if (message === "[CALL_ACCEPTED]" && senderRole !== currentRole) {
            document.getElementById('audioRingOut').pause();
            document.getElementById('callStatus').innerText = '00:01 - Đã kết nối';
            document.getElementById('callPulse').classList.remove('animate-ping');
            callState = 'connected';
            let sec = 1;
            window.callTimer = setInterval(() => {
                sec++;
                document.getElementById('callStatus').innerText = `${String(Math.floor(sec/60)).padStart(2, '0')}:${String(sec%60).padStart(2, '0')}`;
            }, 1000);
            return;
        }

        // Nhận tin nhắn Text
        if (senderRole !== currentRole && !message.startsWith('[')) {
            appendMessageToUI('Other', message);
            saveMessageToLocal(rideId, senderRole, message);
            
            const chatBox = document.getElementById('chatBox');
            if (chatBox.classList.contains('hidden')) {
                const badge = currentRole === 'User' ? document.getElementById('chatBadgeUser') : document.getElementById('chatBadgeDriver');
                if(badge) badge.classList.remove('hidden');
            }
        }
    });

    connection.on("ReceiveNewRideRequest", (ride) => { if(role === 'Driver') fetchPendingRides(); });
    await connection.start();
}

function startRealGPS(rideId) {
    if(gpsWatchId) navigator.geolocation.clearWatch(gpsWatchId);
    gpsWatchId = navigator.geolocation.watchPosition(async (pos) => {
        try { if(connection && connection.state === "Connected") await connection.invoke("UpdateLocation", rideId, pos.coords.latitude, pos.coords.longitude); } catch (e) { }
    }, null, { enableHighAccuracy: true });
}

async function checkCurrentRide() {
    try {
        const res = await fetch(`${API_URL}/Ride/current?_=${new Date().getTime()}`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        
        if (res.ok) {
            const data = await res.json();
            
            if (data.IsEmpty || data.isEmpty) {
                activeRideId = null;
                if (currentRole === 'User') {
                    safeClassList('customer-progress', 'add', 'hidden'); safeClassList('form-content', 'remove', 'hidden');
                    if(pickupMarker) pickupMarker.remove(); if(destMarker) destMarker.remove();
                    if(map && map.getSource('route')) { map.removeLayer('route-line'); map.removeSource('route'); }
                } else if (currentRole === 'Driver') {
                    safeClassList('driver-progress', 'add', 'hidden'); safeClassList('driver-idle', 'remove', 'hidden'); fetchPendingRides();
                }
                return;
            }

            activeRideId = data.Id || data.id;

            // ✅ Lấy lịch sử chat khi vừa vào trang
            if (activeRideId !== lastLoadedChatRideId) {
                loadMessagesFromLocal(activeRideId);
                lastLoadedChatRideId = activeRideId;
            }

            if (connection && connection.state === "Connected") { connection.invoke("JoinRideGroup", activeRideId).catch(e=>{}); }

            const pLoc = data.PickupLocation || data.pickupLocation;
            const dLoc = data.Destination || data.destination;
            const status = data.Status || data.status; 
            
            if (currentRole === 'User') { 
                safeClassList('form-content', 'add', 'hidden'); safeClassList('customer-progress', 'remove', 'hidden'); 
                const h3 = document.querySelector('#customer-progress h3');
                const driverCard = document.getElementById('driverInfoCard'); 
                
                if (status === 'Pending') {
                    if(h3) h3.innerText = 'ĐANG TÌM TÀI XẾ...';
                    if(driverCard) driverCard.classList.add('hidden'); 
                } else {
                    if(h3) h3.innerText = 'TÀI XẾ ĐANG ĐẾN';
                    const dInfo = data.DriverInfo || data.driverInfo;
                    if (dInfo && driverCard) {
                        driverCard.classList.remove('hidden'); 
                        document.getElementById('driverName').innerText = dInfo.Name || dInfo.name;
                        document.getElementById('driverRating').innerText = parseFloat(dInfo.Rating || dInfo.rating).toFixed(1); 
                        document.getElementById('driverPhone').innerText = dInfo.Phone || dInfo.phone;
                    }
                }
            }
            else if (currentRole === 'Driver') {
                safeClassList('driver-idle', 'add', 'hidden'); safeClassList('driver-progress', 'remove', 'hidden');
                if(document.getElementById('crPickup')) document.getElementById('crPickup').innerText = pLoc;
                if(document.getElementById('crDest')) document.getElementById('crDest').innerText = dLoc;
                if(document.getElementById('crPrice')) document.getElementById('crPrice').innerText = (data.Price || data.price || 0).toLocaleString();
                
                const cInfo = data.CustomerInfo || data.customerInfo;
                if (cInfo) { document.getElementById('crCustomerName').innerText = cInfo.Name || cInfo.name; }
                startRealGPS(activeRideId);
            }
            restoreRoute(pLoc, dLoc);
        }
    } catch (e) { }
}

async function fetchHistory() {
    const tbody = document.getElementById('historyBody'); if(!tbody) return;
    tbody.innerHTML = `<tr><td colspan="4" class="p-8 text-center text-thuelai"><i class="fas fa-spinner fa-spin text-2xl"></i></td></tr>`;
    try {
        const res = await fetch(`${API_URL}/Ride/history`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) {
            const data = await res.json();
            tbody.innerHTML = data.length ? data.map(r => {
                const statusStr = r.status || r.Status;
                const ratingVal = r.rating || r.Rating;
                let actionHtml = `<span class="px-2 py-1 rounded text-[8px] font-black uppercase ${statusStr === 'Completed' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}">${statusStr}</span>`;
                
                if (statusStr === 'Completed') {
                    if (ratingVal) {
                        actionHtml += `<div class="mt-1.5 text-yellow-500 text-[10px] font-bold"><i class="fas fa-star"></i> ${ratingVal} Sao</div>`;
                    } else {
                        if (currentRole === 'User') { actionHtml += `<div class="mt-1.5"><button onclick="openRatingModal('${r.id || r.Id}')" class="bg-yellow-400 hover:bg-yellow-500 text-white px-3 py-1 rounded-full text-[9px] font-bold shadow-sm transition">Đánh giá</button></div>`; } 
                        else if (currentRole === 'Driver') { actionHtml += `<div class="mt-1.5 text-gray-400 text-[9px] font-bold italic">Chưa đánh giá</div>`; }
                    }
                }
                return `<tr class="border-b border-gray-100"><td class="p-4 text-gray-500 text-[10px]">${new Date(r.createdAt || r.CreatedAt).toLocaleString('vi-VN')}</td><td class="p-4 truncate max-w-[150px] font-bold text-xs uppercase tracking-tighter">● ${r.pickupLocation || r.PickupLocation} <br> ■ ${r.destination || r.Destination}</td><td class="p-4 font-black text-right text-xs">${(r.price || r.Price).toLocaleString()}đ</td><td class="p-4 text-center align-middle">${actionHtml}</td></tr>`;
            }).join('') : '<tr><td colspan="4" class="p-8 text-center text-gray-400">Chưa có lịch sử chuyến đi.</td></tr>';
        }
    } catch(e) {}
}

// 🌐 THUẬT TOÁN HAVERSINE: TÍNH KHOẢNG CÁCH GIỮA 2 TỌA ĐỘ GPS (TÍNH BẰNG KM)
function getDistanceInKm(lat1, lon1, lat2, lon2) {
    const R = 6371; // Bán kính trái đất (km)
    const dLat = (lat2 - lat1) * (Math.PI / 180);
    const dLon = (lon2 - lon1) * (Math.PI / 180);
    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.cos(lat1 * (Math.PI / 180)) * Math.cos(lat2 * (Math.PI / 180)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
    return R * (2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a)));
}

async function fetchPendingRides() {
    const area = document.getElementById('logArea'); if(!area) return;
    const res = await fetch(`${API_URL}/Ride/pending`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
    if (res.ok) {
        const data = await res.json();

        // ✅ THUẬT TOÁN SMART MATCHING: CHỈ LỌC CÁC CUỐC XE TRONG BÁN KÍNH 3KM
        const nearbyRides = data.filter(r => {
            if (!window.driverCurrentCoords) return true; // Nếu Tài xế chưa bật GPS, tạm cho hiện hết
            const dist = getDistanceInKm(
                window.driverCurrentCoords[1], window.driverCurrentCoords[0], // Tọa độ Tài xế
                r.pickupLat || r.PickupLat, r.pickupLng || r.PickupLng        // Tọa độ Khách
            );
            return dist <= 3.0; // QUAN TRỌNG: Điều chỉnh bán kính quét tại đây (3.0 = 3km)
        });

        area.innerHTML = nearbyRides.length ? "" : '<p class="text-center py-6 text-gray-400 text-[10px] font-bold uppercase tracking-widest">Đang tìm cuốc quanh bạn (3km)...</p>';
        
        nearbyRides.forEach(r => {
            // Tính toán và hiển thị khoảng cách từ Tài xế đến Khách hàng lên giao diện
            let distUI = "";
            if (window.driverCurrentCoords) {
                const dist = getDistanceInKm(window.driverCurrentCoords[1], window.driverCurrentCoords[0], r.pickupLat || r.PickupLat, r.pickupLng || r.PickupLng);
                distUI = `<span class="bg-blue-50 text-blue-600 px-2 py-0.5 rounded text-[9px] ml-2 shadow-sm border border-blue-100"><i class="fas fa-location-arrow"></i> Cách bạn ${dist.toFixed(1)} km</span>`;
            }

            const isManual = (r.transmissionType || r.TransmissionType) === 1;
            area.insertAdjacentHTML('afterbegin', `
            <div class="bg-white p-4 rounded-xl border border-gray-200 shadow-sm transition hover:shadow-md border-l-4 border-l-thuelai">
                <div class="flex justify-between items-center mb-3">
                    <span class="text-lg font-black text-thuelai">${(r.price || r.Price).toLocaleString()} đ</span>
                    <span class="text-[10px] ${isManual ? 'bg-red-50 text-red-600 border border-red-200' : 'bg-green-50 text-green-700 border border-green-200'} px-2 py-1 rounded font-bold">${isManual ? "⚙️ SỐ SÀN (Cần B2)" : "🚗 SỐ TỰ ĐỘNG"}</span>
                </div>
                <div class="space-y-1 mb-3">
                    <p class="text-[10px] font-bold text-gray-600 line-clamp-1">📍 Đón: ${r.pickupLocation || r.PickupLocation} ${distUI}</p>
                    <p class="text-[10px] font-bold text-gray-600 line-clamp-1 mt-1">🏁 Đến: ${r.destination || r.Destination}</p>
                </div>
                <button onclick="acceptRide('${r.id || r.Id}')" class="w-full bg-thuelai hover:bg-thuelai-hover text-white py-2.5 rounded-lg font-bold text-xs uppercase transition shadow-sm">Nhận chuyến ngay</button>
            </div>`);
        });
    }
}

async function bookRide() {
    if(!pickupCoords || !destCoords) return Swal.fire({...swalOpts, icon: 'warning', text: 'Chọn Điểm đón và Điểm đến trên bản đồ!'});
    const btn = document.getElementById('btnBook'); if(!btn) return;
    btn.disabled = true; btn.innerHTML = '<i class="fas fa-circle-notch fa-spin"></i>';
    try {
        const res = await fetch(`${API_URL}/Ride/book`, { 
            method: 'POST', 
            headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` }, 
            body: JSON.stringify({ 
                pickupLocation: document.getElementById('txtPickup').value, 
                destination: document.getElementById('txtDestination').value, 
                distance: parseFloat(document.getElementById('displayDist').innerText), 
                vehicleType: 0, 
                transmissionType: parseInt(document.getElementById('carTransmission').value),
                paymentMethod: document.getElementById('paymentMethod').value,
                pickupLat: pickupCoords[1], // ✅ GỬI KÈM TỌA ĐỘ
                pickupLng: pickupCoords[0]  // ✅ GỬI KÈM TỌA ĐỘ
            }) 
        });
        if (res.ok) { 
            const data = await res.json(); activeRideId = data.RideId; 
            safeClassList('form-content', 'add', 'hidden'); safeClassList('customer-progress', 'remove', 'hidden');
            const h3 = document.querySelector('#customer-progress h3'); if (h3) h3.innerText = 'ĐANG TÌM TÀI XẾ...';
        } else Swal.fire({...swalOpts, icon: 'error', text: 'Không thể đặt xe lúc này.'}); 
    } catch (e) { Swal.fire({...swalOpts, icon: 'error', text: 'Lỗi mạng.'}); } finally { btn.disabled = false; btn.innerHTML = 'XÁC NHẬN GỌI XE'; }
}

async function acceptRide(id) {
    const res = await fetch(`${API_URL}/Driver/accept-ride/${id}`, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
    if (res.ok) checkCurrentRide(); else Swal.fire({...swalOpts, icon: 'error', text: await res.text() || 'Không thể nhận chuyến.'});
}

async function completeRide() { 
    if(!activeRideId) return;
    await fetch(`${API_URL}/Driver/complete-ride/${activeRideId}`, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } }); 
    activeRideId = null; checkCurrentRide(); 
}

async function cancelRide() { 
    if (!activeRideId) { safeClassList('customer-progress', 'add', 'hidden'); safeClassList('form-content', 'remove', 'hidden'); return; }
    const tempId = activeRideId; activeRideId = null; 
    safeClassList('customer-progress', 'add', 'hidden'); safeClassList('form-content', 'remove', 'hidden');
    try { await fetch(`${API_URL}/Ride/cancel/${tempId}`, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } }); } catch(e) {}
}

window.openRatingModal = function(id) {
    currentRatingRideId = id; setRating(0); document.getElementById('txtComment').value = ''; showModal('ratingModal');
}
function setRating(n) { selectedRating = n; document.querySelectorAll('#starContainer i').forEach((s, i) => { s.className = i < n ? 'fas fa-star text-yellow-400 transition' : 'fas fa-star text-gray-200 transition'; }); }
async function submitRating() { 
    if(selectedRating === 0) return Swal.fire({...swalOpts, icon: 'warning', text: 'Vui lòng chọn số sao!'});
    const btn = document.querySelector('#ratingModal button'); const originalText = btn.innerHTML;
    btn.disabled = true; btn.innerHTML = '<i class="fas fa-circle-notch fa-spin"></i> ĐANG GỬI...';
    try {
        const res = await fetch(`${API_URL}/Ride/rate`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` }, body: JSON.stringify({ rideId: currentRatingRideId, rating: selectedRating, comment: document.getElementById('txtComment').value }) }); 
        if (res.ok) { hideModal('ratingModal'); Swal.fire({...swalOpts, icon: 'success', text: 'Cảm ơn bạn đã đánh giá!'}).then(() => fetchHistory()); } 
        else { Swal.fire({...swalOpts, icon: 'error', text: 'Lỗi gửi đánh giá.'}); }
    } catch(e) { Swal.fire({...swalOpts, icon: 'error', text: 'Mất kết nối mạng.'}); } finally { btn.disabled = false; btn.innerHTML = originalText; }
}

function toggleAccountMenu(e) { e.stopPropagation(); const dd = document.getElementById('accountDropdown'); if(dd) dd.classList.toggle('hidden'); }
function logout() { localStorage.removeItem('lmd_token'); location.reload(); }
function showModal(id) { const el = document.getElementById(id); if(el) { el.classList.remove('hidden', 'modal-inactive'); setTimeout(() => el.classList.add('modal-active'), 10); } }
function hideModal(id) { const el = document.getElementById(id); if(el) { el.classList.remove('modal-active'); el.classList.add('modal-inactive'); setTimeout(() => el.classList.add('hidden'), 200); } }
function toggleDriverFields() { const el = document.getElementById('driverExtraFields'); if(el) el.className = document.getElementById('regRole').value === 'Driver' ? 'block' : 'hidden'; }

window.onload = () => { checkAuthState(); };
window.onclick = () => { const dd = document.getElementById('accountDropdown'); if(dd) dd.classList.add('hidden'); };


// --- HỆ THỐNG CHAT & GỌI ĐIỆN REAL-TIME ---
function saveMessageToLocal(rideId, senderRole, msg) {
    const key = 'chat_' + rideId;
    let history = JSON.parse(localStorage.getItem(key) || '[]');
    history.push({ senderRole, msg });
    localStorage.setItem(key, JSON.stringify(history));
}

function loadMessagesFromLocal(rideId) {
    const key = 'chat_' + rideId;
    let history = JSON.parse(localStorage.getItem(key) || '[]');
    const container = document.getElementById('chatMessages');
    container.innerHTML = '<div class="text-center text-[10px] text-gray-400 my-2 font-bold uppercase tracking-widest border-b border-gray-200 pb-2 mx-10">Bắt đầu trò chuyện</div>';
    history.forEach(item => {
        appendMessageToUI(item.senderRole === currentRole ? 'Me' : 'Other', item.msg);
    });
}

function toggleChat() {
    const box = document.getElementById('chatBox');
    const overlay = document.getElementById('chatOverlay');
    box.classList.toggle('hidden'); overlay.classList.toggle('hidden');
    if(!box.classList.contains('hidden')) {
        document.getElementById('txtChatMsg').focus();
        scrollToBottomChat();
        const badge = currentRole === 'User' ? document.getElementById('chatBadgeUser') : document.getElementById('chatBadgeDriver');
        if(badge) badge.classList.add('hidden');
    }
}

async function sendChat() {
    const input = document.getElementById('txtChatMsg');
    const msg = input.value.trim();
    if(!msg || !activeRideId) return;

    input.value = '';
    appendMessageToUI('Me', msg); 
    saveMessageToLocal(activeRideId, currentRole, msg); // Lưu tin nhắn của mình
    
    try { await connection.invoke("SendChatMessage", activeRideId, currentRole, msg); } catch(e) {}
}

function appendMessageToUI(sender, msg) {
    const container = document.getElementById('chatMessages');
    const isMe = sender === 'Me';
    const html = `
        <div class="flex ${isMe ? 'justify-end' : 'justify-start'}">
            <div class="max-w-[75%] px-4 py-2.5 text-sm ${isMe ? 'bg-thuelai text-white rounded-2xl rounded-tr-sm' : 'bg-white border border-gray-200 text-gray-800 rounded-2xl rounded-tl-sm shadow-sm'}">
                ${msg}
            </div>
        </div>
    `;
    container.insertAdjacentHTML('beforeend', html);
    scrollToBottomChat();
}

function scrollToBottomChat() {
    const container = document.getElementById('chatMessages');
    container.scrollTop = container.scrollHeight;
}
// --- ADMIN DASHBOARD ---

// Hàm chuyển Tab
window.switchAdminTab = function(tabName) {
    // Đổi màu Sidebar
    ['dashboard', 'drivers', 'rides'].forEach(t => {
        const btn = document.getElementById('nav-tab-' + t);
        if (t === tabName) {
            btn.className = 'w-full flex items-center gap-3 px-4 py-3 bg-green-50 text-thuelai rounded-xl font-bold transition';
        } else {
            btn.className = 'w-full flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 rounded-xl font-bold transition';
        }
        // Ẩn/Hiện Content
        const content = document.getElementById('admin-tab-' + t);
        if(t === tabName) content.classList.remove('hidden');
        else content.classList.add('hidden');
    });

    // Load data tương ứng
    if (tabName === 'dashboard') loadAdminDashboard();
    if (tabName === 'drivers') loadAdminAllDrivers();
    if (tabName === 'rides') loadAdminAllRides();
}

// 1. Tải Tổng quan
window.loadAdminDashboard = async function() {
    try {
        const statRes = await fetch(`${API_URL}/Admin/stats`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (statRes.ok) {
            const stats = await statRes.json();
            document.getElementById('adTotalUsers').innerText = stats.totalUsers || 0;
            document.getElementById('adTotalDrivers').innerText = stats.totalDrivers || 0;
            document.getElementById('adTotalRides').innerText = stats.totalRides || 0;
            document.getElementById('adTotalRevenue').innerText = (stats.totalRevenue || 0).toLocaleString() + ' đ';
        }

        const penRes = await fetch(`${API_URL}/Admin/pending-drivers`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        const tbody = document.getElementById('adPendingDrivers');
        if (penRes.ok) {
            const drivers = await penRes.json();
            tbody.innerHTML = drivers.length ? drivers.map(d => `
                <tr class="border-b border-gray-50 hover:bg-gray-50">
                    <td class="p-4 font-bold text-gray-800"><i class="fas fa-user-circle text-gray-300 mr-2 text-lg"></i> ${d.email || d.Email}</td>
                    <td class="p-4 text-center"><span class="px-3 py-1 bg-blue-50 text-blue-600 rounded-full text-[10px] font-black tracking-widest">${d.licenseType || d.LicenseType}</span></td>
                    <td class="p-4 text-right">
                        <button onclick="viewDriverDetails('${d.id || d.Id}')" class="bg-blue-50 hover:bg-blue-100 text-blue-600 px-4 py-1.5 rounded-lg text-xs font-bold shadow-sm transition"><i class="fas fa-search mr-1"></i> Kiểm duyệt</button>
                    </td>
                </tr>
            `).join('') : '<tr><td colspan="3" class="p-8 text-center text-gray-400 font-medium uppercase text-xs">Không có hồ sơ chờ duyệt.</td></tr>';
        }
    } catch(e) {}
}
window.viewDriverDetails = async function(id) {
    Swal.fire({ title: 'Đang tải hồ sơ...', didOpen: () => Swal.showLoading() });
    try {
        const res = await fetch(`${API_URL}/Admin/driver/${id}`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) {
            const d = await res.json();
            Swal.close();
            document.getElementById('detailDriverEmail').innerText = d.email || d.Email;
            document.getElementById('detailDriverLicense').innerText = "Hạng " + (d.licenseType || d.LicenseType);
            
            const img = document.getElementById('detailDriverImg');
            const noImg = document.getElementById('detailDriverNoImg');
            const imgData = d.licenseImage || d.LicenseImage;
            
            if (imgData) {
                img.src = imgData; img.classList.remove('hidden'); noImg.classList.add('hidden');
            } else {
                img.src = ''; img.classList.add('hidden'); noImg.classList.remove('hidden');
            }
            
            // Gắn sự kiện cho nút Duyệt bên trong Modal
            const btn = document.getElementById('btnApproveDetail');
            btn.onclick = () => { hideModal('adminDriverDetailModal'); approveDriver(id); };
            
            showModal('adminDriverDetailModal');
        } else {
            Swal.fire({...swalOpts, icon: 'error', text: 'Không tải được chi tiết hồ sơ.'});
        }
    } catch(e) { Swal.fire({...swalOpts, icon: 'error', text: 'Lỗi mạng.'}); }
}
window.approveDriver = async function(id) {
    Swal.fire({ title: 'Đang xử lý...', didOpen: () => Swal.showLoading() });
    try {
        const res = await fetch(`${API_URL}/Admin/approve-driver/${id}`, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) { Swal.fire({...swalOpts, icon: 'success', text: 'Đã duyệt!'}); loadAdminDashboard(); }
    } catch(e) {}
}

// 2. Tải Tất cả Tài xế
window.loadAdminAllDrivers = async function() {
    const tbody = document.getElementById('adAllDrivers');
    tbody.innerHTML = `<tr><td colspan="4" class="p-8 text-center"><i class="fas fa-spinner fa-spin text-thuelai text-2xl"></i></td></tr>`;
    try {
        const res = await fetch(`${API_URL}/Admin/all-drivers`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) {
            const data = await res.json();
            tbody.innerHTML = data.length ? data.map(d => {
                const isApp = d.isApproved || d.IsApproved;
                const isAct = d.isActive || d.IsActive;
                return `
                <tr class="border-b border-gray-50 hover:bg-gray-50">
                    <td class="p-4">
                        <p class="font-bold text-gray-800">${d.email || d.Email}</p>
                        <p class="text-[10px] font-bold text-gray-400">BẰNG ${d.licenseType || d.LicenseType}</p>
                    </td>
                    <td class="p-4 font-black text-thuelai">${(d.balance || d.Balance || 0).toLocaleString()} đ</td>
                    <td class="p-4 text-center">
                        ${isApp ? '<span class="px-2 py-1 bg-green-100 text-green-700 rounded text-[9px] font-black uppercase">Đã Duyệt</span>' : '<span class="px-2 py-1 bg-yellow-100 text-yellow-700 rounded text-[9px] font-black uppercase">Chờ Duyệt</span>'}
                        ${!isAct ? '<span class="px-2 py-1 bg-red-100 text-red-700 rounded text-[9px] font-black uppercase ml-1">Đang Khóa</span>' : ''}
                    </td>
                    <td class="p-4 text-right">
                        <button onclick="toggleDriverStatus('${d.id || d.Id}')" class="px-3 py-1.5 rounded-lg text-xs font-bold text-white transition ${isAct ? 'bg-red-500 hover:bg-red-600' : 'bg-gray-800 hover:bg-gray-900'}">
                            ${isAct ? '<i class="fas fa-lock mr-1"></i> Khóa' : '<i class="fas fa-unlock mr-1"></i> Mở khóa'}
                        </button>
                    </td>
                </tr>`;
            }).join('') : '<tr><td colspan="4" class="p-8 text-center">Trống</td></tr>';
        }
    } catch(e) {}
}

window.toggleDriverStatus = async function(id) {
    try {
        const res = await fetch(`${API_URL}/Admin/toggle-driver/${id}`, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) { loadAdminAllDrivers(); }
    } catch(e) {}
}

// 3. Tải Tất cả Cuốc xe
window.loadAdminAllRides = async function() {
    const tbody = document.getElementById('adAllRides');
    tbody.innerHTML = `<tr><td colspan="4" class="p-8 text-center"><i class="fas fa-spinner fa-spin text-thuelai text-2xl"></i></td></tr>`;
    try {
        const res = await fetch(`${API_URL}/Admin/all-rides`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('lmd_token')}` } });
        if (res.ok) {
            const data = await res.json();
            tbody.innerHTML = data.length ? data.map(r => `
                <tr class="border-b border-gray-50 hover:bg-gray-50">
                    <td class="p-4">
                        <p class="text-[10px] text-gray-400 mb-1">${new Date(r.createdAt || r.CreatedAt).toLocaleString('vi-VN')}</p>
                        <p class="font-bold text-gray-800 truncate max-w-[200px]">📍 ${r.pickupLocation || r.PickupLocation}</p>
                        <p class="font-bold text-gray-500 truncate max-w-[200px]">🏁 ${r.destination || r.Destination}</p>
                    </td>
                    <td class="p-4">
                        <p class="font-semibold text-gray-800"><span class="text-[10px] text-gray-400 uppercase">Khách:</span> ${r.customerEmail || r.CustomerEmail}</p>
                        <p class="font-semibold text-thuelai"><span class="text-[10px] text-gray-400 uppercase">Tài:</span> ${r.driverEmail || r.DriverEmail}</p>
                    </td>
                    <td class="p-4">
                        <p class="font-black text-gray-800">${(r.price || r.Price).toLocaleString()} đ</p>
                        <p class="text-[10px] font-bold text-blue-600 bg-blue-50 inline-block px-1.5 py-0.5 rounded mt-1">${(r.paymentMethod || r.PaymentMethod) === 'Wallet' ? 'Ví LMD Pay' : 'Tiền mặt'}</p>
                    </td>
                    <td class="p-4 text-center align-middle">
                        <span class="px-2 py-1 rounded text-[9px] font-black uppercase ${(r.status || r.Status) === 'Completed' ? 'bg-green-100 text-green-700' : ((r.status || r.Status) === 'Cancelled' ? 'bg-red-100 text-red-700' : 'bg-yellow-100 text-yellow-700')}">${r.status || r.Status}</span>
                    </td>
                </tr>
            `).join('') : '<tr><td colspan="4" class="p-8 text-center text-gray-400">Hệ thống chưa có chuyến đi nào.</td></tr>';
        }
    } catch(e) {}
}
// LOGIC GỌI ĐIỆN MÔ PHỎNG
let callState = 'idle'; 

window.startWebCall = function() {
    if(!activeRideId) return;
    const targetName = currentRole === 'User' ? document.getElementById('driverName').innerText : document.getElementById('crCustomerName').innerText;
    document.getElementById('callName').innerText = targetName || 'Người dùng';
    document.getElementById('callStatus').innerText = 'Đang đổ chuông...';
    
    document.getElementById('callActionsOut').classList.remove('hidden');
    document.getElementById('callActionsIn').classList.add('hidden');
    document.getElementById('callPulse').classList.replace('bg-blue-500', 'bg-green-500');
    
    document.getElementById('webCallModal').classList.remove('hidden');
    document.getElementById('webCallModal').classList.add('flex');
    
    document.getElementById('audioRingOut').play().catch(e=>{});
    callState = 'calling';
    connection.invoke("SendChatMessage", activeRideId, currentRole, "[CALL_REQUEST]").catch(e=>{});
}

window.showIncomingCall = function() {
    const targetName = currentRole === 'User' ? document.getElementById('driverName').innerText : document.getElementById('crCustomerName').innerText;
    document.getElementById('callName').innerText = targetName || 'Người dùng';
    document.getElementById('callStatus').innerText = 'Cuộc gọi đến...';
    
    document.getElementById('callActionsOut').classList.add('hidden');
    document.getElementById('callActionsIn').classList.remove('hidden');
    document.getElementById('callPulse').classList.replace('bg-green-500', 'bg-blue-500');

    document.getElementById('webCallModal').classList.remove('hidden');
    document.getElementById('webCallModal').classList.add('flex');
    
    document.getElementById('audioRingIn').play().catch(e=>{});
    callState = 'ringing';
}

window.endWebCall = function() {
    hideCallModal();
    if(callState !== 'idle') { connection.invoke("SendChatMessage", activeRideId, currentRole, "[CALL_ENDED]").catch(e=>{}); }
    callState = 'idle';
}

window.acceptWebCall = function() {
    document.getElementById('audioRingIn').pause();
    document.getElementById('callStatus').innerText = '00:01 - Đang kết nối thoại...';
    document.getElementById('callActionsIn').classList.add('hidden');
    document.getElementById('callActionsOut').classList.remove('hidden'); 
    document.getElementById('callPulse').classList.remove('animate-ping');
    callState = 'connected';
    connection.invoke("SendChatMessage", activeRideId, currentRole, "[CALL_ACCEPTED]").catch(e=>{});
    
    let sec = 1;
    window.callTimer = setInterval(() => {
        sec++;
        document.getElementById('callStatus').innerText = `${String(Math.floor(sec/60)).padStart(2, '0')}:${String(sec%60).padStart(2, '0')}`;
    }, 1000);
}

window.hideCallModal = function() {
    document.getElementById('webCallModal').classList.add('hidden');
    document.getElementById('webCallModal').classList.remove('flex');
    document.getElementById('audioRingOut').pause(); document.getElementById('audioRingIn').pause();
    document.getElementById('callPulse').classList.add('animate-ping');
    if(window.callTimer) clearInterval(window.callTimer);
    callState = 'idle';
}