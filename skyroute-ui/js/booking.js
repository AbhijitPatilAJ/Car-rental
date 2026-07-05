// booking.js — Booking form logic with currency selection

const params  = new URLSearchParams(window.location.search);
const vehicle = JSON.parse(sessionStorage.getItem('selectedVehicle') || 'null');
const pickup  = params.get('pickup') || '';
const from    = params.get('from')   || '';
const to      = params.get('to')     || '';

// If no vehicle data redirect back
if (!vehicle) { window.location.href = 'index.html'; }

const pickupType      = classifyCity(pickup);
let   selectedCurrency = 'INR';

// ── Populate Summary Sidebar ──────────────────────────────────────────────────
function populateSummary() {
  document.getElementById('sum-badge').textContent     = `${vehicle.provider === 'PremiumDrive' ? '⭐' : '💰'} ${vehicle.provider}`;
  document.getElementById('sum-badge').className       = `provider-badge ${vehicle.provider === 'PremiumDrive' ? 'badge-premium' : 'badge-budget'}`;
  document.getElementById('sum-name').textContent      = vehicle.vehicleName || vehicle.category;
  document.getElementById('sum-category').textContent  = vehicle.category;
  document.getElementById('sum-vehicle').textContent   = vehicle.vehicleId;
  document.getElementById('sum-pickup').textContent    = pickup;
  document.getElementById('sum-from').textContent      = formatDate(from);
  document.getElementById('sum-to').textContent        = formatDate(to);

  const nights = Math.round((new Date(to) - new Date(from)) / 86400000);
  document.getElementById('sum-nights').textContent    = `${nights} night${nights !== 1 ? 's' : ''}`;
  document.getElementById('sum-insurance').textContent = vehicle.insuranceType;
  document.getElementById('sum-cancel').textContent    = vehicle.cancellationPolicy;

  updateCurrencyDisplay();
}

// Helper to count weekend and weekday nights
function countBookingNightTypes(fromStr, toStr) {
  let weekendNights = 0, weekdayNights = 0;
  let cur = new Date(fromStr + 'T00:00:00');
  const end = new Date(toStr + 'T00:00:00');
  while (cur < end) {
    const day = cur.getDay(); // 0=Sun, 5=Fri, 6=Sat
    if (day === 0 || day === 5 || day === 6) weekendNights++;
    else weekdayNights++;
    cur.setDate(cur.getDate() + 1);
  }
  return { weekendNights, weekdayNights };
}

// ── Update price display when currency changes ────────────────────────────────
function updateCurrencyDisplay() {
  const isPremium = vehicle.provider === 'PremiumDrive';
  const inrDaily = vehicle.dailyRate;
  const inrTotal = vehicle.totalPrice;

  if (selectedCurrency === 'USD') {
    document.getElementById('sum-daily').textContent = formatUSD(inrToUsd(inrDaily));
    document.getElementById('sum-total').textContent = formatUSD(inrToUsd(inrTotal));
    document.getElementById('sum-inr-ref').style.display = 'block';
    document.getElementById('sum-inr-ref').textContent   = `(${formatINR(inrTotal)} INR)`;
    if (document.getElementById('sum-currency-row')) {
      document.getElementById('sum-currency-row').style.display = '';
      document.getElementById('sum-currency-display').textContent = `USD  (1 USD = ₹${USD_RATE})`;
    }
  } else {
    document.getElementById('sum-daily').textContent = formatINR(inrDaily);
    document.getElementById('sum-total').textContent = formatINR(inrTotal);
    document.getElementById('sum-inr-ref').style.display = 'none';
    if (document.getElementById('sum-currency-row')) {
      document.getElementById('sum-currency-row').style.display = 'none';
    }
  }

  // Render surcharge details box in sidebar dynamically
  const box = document.getElementById('sum-surcharge-box');
  if (box && from && to) {
    const formatFn = selectedCurrency === 'USD' ? (val) => formatUSD(inrToUsd(val)) : formatINR;
    if (!isPremium) {
      const { weekendNights, weekdayNights } = countBookingNightTypes(from, to);
      const avgRate = Math.round(inrTotal / (weekendNights + weekdayNights));
      if (weekendNights > 0) {
        box.innerHTML = `
          <div class="surcharge-box" style="margin: 10px 0;">
            ${weekdayNights > 0 ? `
            <div class="surcharge-row">
              <span>🌙 ${weekdayNights} weeknight${weekdayNights !== 1 ? 's' : ''}</span>
              <span>${formatFn(inrDaily)}/night</span>
            </div>` : ''}
            <div class="surcharge-row weekend-row">
              <span>🎉 ${weekendNights} weekend night${weekendNights !== 1 ? 's' : ''} (+20%)</span>
              <span>${formatFn(Math.round(inrDaily * 1.2))}/night</span>
            </div>
            <div class="surcharge-row effective-row">
              <span>📊 Avg per night</span>
              <span>${formatFn(avgRate)}</span>
            </div>
          </div>`;
      } else {
        box.innerHTML = `
          <div class="surcharge-box no-surcharge" style="margin: 10px 0;">
            <div class="surcharge-row">
              <span>🌙 ${weekdayNights} weeknight${weekdayNights !== 1 ? 's' : ''} (no surcharge)</span>
              <span>${formatFn(inrDaily)}/night</span>
            </div>
          </div>`;
      }
    } else {
      box.innerHTML = `
        <div class="surcharge-box flat-rate" style="margin: 10px 0;">
          <div class="surcharge-row">
            <span>📋 Flat rate — same every night</span>
            <span>${formatFn(inrDaily)}/night</span>
          </div>
        </div>`;
    }
  }
}


// ── Currency toggle — show only for international pickups ─────────────────────
function initCurrencyToggle() {
  const group = document.getElementById('currency-group');
  if (!group) return;

  if (pickupType === 'international') {
    group.style.display = '';   // show the toggle

    document.querySelectorAll('.currency-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        selectedCurrency = btn.dataset.currency;
        document.getElementById('selected-currency').value = selectedCurrency;

        // Update active state
        document.querySelectorAll('.currency-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');

        updateCurrencyDisplay();
      });
    });
  } else {
    // Domestic — only INR, hide the toggle
    group.style.display = 'none';
    selectedCurrency = 'INR';
  }
}

// ── Document type guidance ────────────────────────────────────────────────────
function initDocumentGuidance() {
  const docHelpEl = document.getElementById('doc-type-help');
  const docTypeEl = document.getElementById('doc-type');
  if (!docHelpEl || !docTypeEl) return;

  if (pickupType === 'international') {
    docHelpEl.textContent = `⚠️ ${pickup} is an international location — Passport required.`;
    docHelpEl.style.color = 'var(--warning)';
    docTypeEl.value = 'Passport';
    docTypeEl.querySelector('option[value="NationalId"]').disabled = true;
  } else if (pickupType === 'domestic') {
    docHelpEl.textContent = `✓ ${pickup} is a domestic location — Passport or National ID accepted.`;
    docHelpEl.style.color = 'var(--success)';
    docTypeEl.querySelector('option[value="NationalId"]').disabled = false;
  } else {
    docHelpEl.textContent = '';
  }
}

// ── Form submission ───────────────────────────────────────────────────────────
document.getElementById('booking-form').addEventListener('submit', async e => {
  e.preventDefault();

  // Clear previous errors
  document.querySelectorAll('.field-error').forEach(el => el.textContent = '');

  const driverName   = document.getElementById('driver-name').value.trim();
  const documentType = document.getElementById('doc-type').value;
  const documentNumber = document.getElementById('doc-number').value.trim();

  // Client-side document validation (server also validates)
  const validationMsg = validateDocument(pickup, documentType);
  if (validationMsg) {
    document.getElementById('doc-number-error').textContent = validationMsg;
    return;
  }

  const btn = document.getElementById('btn-confirm');
  btn.disabled = true;
  btn.textContent = 'Processing…';

  try {
    const result = await api.book({
      vehicleId:          vehicle.vehicleId,
      provider:           vehicle.provider,
      pickup,
      from,
      to,
      totalPrice:         vehicle.totalPrice,   // always INR
      insuranceType:      vehicle.insuranceType,
      cancellationPolicy: vehicle.cancellationPolicy,
      driverName,
      documentType,
      documentNumber,
      currency:           selectedCurrency,     // INR or USD
    });

    sessionStorage.setItem('bookingConfirmation', JSON.stringify({
      ...result,
      selectedCurrency,
      vehicleName: vehicle.vehicleName,
    }));
    sessionStorage.removeItem('selectedVehicle');
    window.location.href = 'confirmation.html';

  } catch (err) {
    btn.disabled = false;
    btn.textContent = 'Confirm Booking →';

    if (err.status === 422 && err.body) {
      document.getElementById('doc-number-error').textContent = err.body.message;
    } else {
      document.getElementById('doc-number-error').textContent = err.message || 'Booking failed. Please try again.';
    }
  }
});

// ── Init ──────────────────────────────────────────────────────────────────────
populateSummary();
initCurrencyToggle();
initDocumentGuidance();
