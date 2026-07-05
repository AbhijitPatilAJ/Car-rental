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

// ── Update price display when currency changes ────────────────────────────────
function updateCurrencyDisplay() {
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
