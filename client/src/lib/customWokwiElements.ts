if (typeof window !== 'undefined') {
    // ----------------------------------------------------------------------
    // HC-05 Bluetooth Module
    // ----------------------------------------------------------------------
    class CustomHC05Element extends HTMLElement {
        pinInfo = [
            { name: "STATE", x: 20, y: 10 },
            { name: "RXD", x: 40, y: 10 },
            { name: "TXD", x: 60, y: 10 },
            { name: "GND", x: 80, y: 10 },
            { name: "VCC", x: 100, y: 10 },
            { name: "EN", x: 120, y: 10 }
        ];

        constructor() {
            super();
            this.attachShadow({ mode: 'open' });
            this.shadowRoot!.innerHTML = `
                <style>
                    .board { 
                        width: 140px; 
                        height: 200px; 
                        background-color: #1e3a8a; /* Deep blue */
                        border-radius: 8px; 
                        position: relative; 
                        color: white; 
                        font-family: monospace; 
                        display: flex; 
                        flex-direction: column; 
                        align-items: center; 
                        border: 2px solid #172554;
                        box-shadow: 0 4px 6px rgba(0,0,0,0.3);
                    }
                    .header {
                        width: 100%;
                        height: 50px;
                        background: #111;
                        border-top-left-radius: 8px;
                        border-top-right-radius: 8px;
                        position: relative;
                    }
                    .chip { 
                        width: 60px; 
                        height: 60px; 
                        background-color: #222; 
                        position: absolute; 
                        top: 70px; 
                        border-radius: 4px; 
                        border: 1px solid #444;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        font-size: 8px;
                        color: #666;
                    }
                    .label { 
                        position: absolute; 
                        bottom: 20px; 
                        font-weight: bold; 
                        font-size: 16px;
                        letter-spacing: 2px;
                    }
                    .pin-label {
                        position: absolute;
                        font-size: 8px;
                        top: 25px;
                        transform: translateX(-50%) rotate(-90deg);
                        transform-origin: left center;
                    }
                    .pin-hole {
                        position: absolute;
                        top: 10px;
                        width: 8px;
                        height: 8px;
                        background: #gold;
                        border-radius: 50%;
                        border: 2px solid #b8860b;
                        transform: translateX(-50%);
                    }
                </style>
                <div class="board">
                    <div class="header">
                        <div class="pin-hole" style="left: 20px; background: #fbbf24; border-color: #b45309;"></div>
                        <div class="pin-hole" style="left: 40px; background: #fbbf24; border-color: #b45309;"></div>
                        <div class="pin-hole" style="left: 60px; background: #fbbf24; border-color: #b45309;"></div>
                        <div class="pin-hole" style="left: 80px; background: #fbbf24; border-color: #b45309;"></div>
                        <div class="pin-hole" style="left: 100px; background: #fbbf24; border-color: #b45309;"></div>
                        <div class="pin-hole" style="left: 120px; background: #fbbf24; border-color: #b45309;"></div>
                        
                        <div class="pin-label" style="left: 20px;">STATE</div>
                        <div class="pin-label" style="left: 40px;">RXD</div>
                        <div class="pin-label" style="left: 60px;">TXD</div>
                        <div class="pin-label" style="left: 80px;">GND</div>
                        <div class="pin-label" style="left: 100px;">VCC</div>
                        <div class="pin-label" style="left: 120px;">EN</div>
                    </div>
                    
                    <!-- Antenna trace zigzag -->
                    <svg width="40" height="20" style="position: absolute; top: 155px; left: 50px;" viewBox="0 0 40 20">
                        <path d="M0,10 L5,0 L10,20 L15,0 L20,20 L25,0 L30,20 L35,0 L40,10" fill="none" stroke="#fbbf24" stroke-width="2"/>
                    </svg>

                    <div class="chip">CSR</div>
                    <div class="label">HC-05</div>
                </div>
            `;
        }
    }

    // ----------------------------------------------------------------------
    // RF Transmitter Module (433MHz)
    // ----------------------------------------------------------------------
    class CustomRFTransmitterElement extends HTMLElement {
        pinInfo = [
            { name: "DATA", x: 20, y: 10 },
            { name: "VCC", x: 40, y: 10 },
            { name: "GND", x: 60, y: 10 }
        ];

        constructor() {
            super();
            this.attachShadow({ mode: 'open' });
            this.shadowRoot!.innerHTML = `
                <style>
                    .board { 
                        width: 80px; 
                        height: 80px; 
                        background-color: #166534; /* Green */
                        border-radius: 4px; 
                        position: relative; 
                        color: white; 
                        font-family: monospace; 
                    }
                    .coil {
                        width: 30px;
                        height: 30px;
                        border: 4px solid #b45309;
                        border-radius: 50%;
                        position: absolute;
                        top: 15px;
                        left: 25px;
                    }
                    .crystal {
                        width: 25px;
                        height: 10px;
                        background: silver;
                        position: absolute;
                        top: 55px;
                        left: 27px;
                        border-radius: 5px;
                    }
                    .pin-hole {
                        position: absolute;
                        top: 10px;
                        width: 6px;
                        height: 6px;
                        background: #fbbf24;
                        border-radius: 50%;
                        transform: translateX(-50%);
                    }
                    .pin-label {
                        position: absolute;
                        font-size: 8px;
                        top: 20px;
                        transform: translateX(-50%);
                    }
                </style>
                <div class="board">
                    <div class="pin-hole" style="left: 20px;"></div>
                    <div class="pin-hole" style="left: 40px;"></div>
                    <div class="pin-hole" style="left: 60px;"></div>
                    
                    <div class="pin-label" style="left: 20px;">DATA</div>
                    <div class="pin-label" style="left: 40px;">VCC</div>
                    <div class="pin-label" style="left: 60px;">GND</div>
                    
                    <div class="coil"></div>
                    <div class="crystal"></div>
                    <div style="position:absolute; bottom:2px; left:10px; font-size:8px; font-weight:bold;">RF-TX</div>
                </div>
            `;
        }
    }

    // ----------------------------------------------------------------------
    // RF Receiver Module (433MHz)
    // ----------------------------------------------------------------------
    class CustomRFReceiverElement extends HTMLElement {
        pinInfo = [
            { name: "VCC", x: 15, y: 10 },
            { name: "DATA", x: 35, y: 10 },
            { name: "DATA2", x: 55, y: 10 },
            { name: "GND", x: 75, y: 10 }
        ];

        constructor() {
            super();
            this.attachShadow({ mode: 'open' });
            this.shadowRoot!.innerHTML = `
                <style>
                    .board { 
                        width: 90px; 
                        height: 160px; 
                        background-color: #166534; /* Green */
                        border-radius: 4px; 
                        position: relative; 
                        color: white; 
                        font-family: monospace; 
                    }
                    .chip {
                        width: 30px;
                        height: 60px;
                        background: #222;
                        position: absolute;
                        top: 40px;
                        left: 30px;
                        border-radius: 2px;
                    }
                    .inductor {
                        width: 15px;
                        height: 15px;
                        border: 3px solid #b45309;
                        border-radius: 50%;
                        position: absolute;
                        top: 120px;
                        left: 37px;
                    }
                    .pin-hole {
                        position: absolute;
                        top: 10px;
                        width: 6px;
                        height: 6px;
                        background: #fbbf24;
                        border-radius: 50%;
                        transform: translateX(-50%);
                    }
                    .pin-label {
                        position: absolute;
                        font-size: 8px;
                        top: 20px;
                        transform: translateX(-50%) rotate(-90deg);
                        transform-origin: left center;
                    }
                </style>
                <div class="board">
                    <div class="pin-hole" style="left: 15px;"></div>
                    <div class="pin-hole" style="left: 35px;"></div>
                    <div class="pin-hole" style="left: 55px;"></div>
                    <div class="pin-hole" style="left: 75px;"></div>
                    
                    <div class="pin-label" style="left: 15px;">VCC</div>
                    <div class="pin-label" style="left: 35px;">DATA</div>
                    <div class="pin-label" style="left: 55px;">DATA</div>
                    <div class="pin-label" style="left: 75px;">GND</div>
                    
                    <div class="chip"></div>
                    <div class="inductor"></div>
                    <div style="position:absolute; bottom:5px; left:25px; font-size:10px; font-weight:bold;">RF-RX</div>
                </div>
            `;
        }
    }

    // Register elements
    if (!customElements.get('custom-hc-05')) customElements.define('custom-hc-05', CustomHC05Element);
    if (!customElements.get('custom-rf-transmitter')) customElements.define('custom-rf-transmitter', CustomRFTransmitterElement);
    if (!customElements.get('custom-rf-receiver')) customElements.define('custom-rf-receiver', CustomRFReceiverElement);
}
