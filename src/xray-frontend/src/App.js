import "milligram";
import React, { useState } from "react";

function App() {
  const [imageSrc, setImageSrc] = useState(null);
  const [loading, setLoading] = useState(false);
  const [form, setForm] = useState({
    wavelength: "",
    resolution: "",
    halfWidth: "",
    austeniteContent: "",
    carbonContent: "",
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async () => {
    setLoading(true);
    setImageSrc(null);

    try {
      const response = await fetch("http://localhost:5004/api/analysis/analyze", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          wavelength: parseFloat(form.wavelength),
          resolution: parseFloat(form.resolution),
          halfWidth: parseFloat(form.halfWidth),
          austeniteContent: parseFloat(form.austeniteContent),
          carbonContent: parseFloat(form.carbonContent),
        }),
      });

      if (!response.ok) {
        alert("Failed to generate image");
        setLoading(false);
        return;
      }

      const blob = await response.blob();
      const imageUrl = URL.createObjectURL(blob);
      setImageSrc(imageUrl);
    } catch (error) {
      alert("Error fetching the image");
    }

    setLoading(false);
  };

  return (
    <div className="container" style={{ maxWidth: 1800, marginTop: 40 }}>
      <h2>X-Ray Diffraction Analysis</h2>

      <div style={{ display: "flex", gap: 40, alignItems: "flex-start" }}>
        {/* Form on the left */}
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleSubmit();
          }}
          style={{ flex: 1, maxWidth: 350 }}
        >
          <fieldset>
            <label htmlFor="wavelength">Wavelength</label>
            <input
              type="number"
              id="wavelength"
              name="wavelength"
              value={form.wavelength}
              onChange={handleChange}
              placeholder="Enter wavelength"
              required
            />

            <label htmlFor="resolution">Resolution</label>
            <input
              type="number"
              id="resolution"
              name="resolution"
              value={form.resolution}
              onChange={handleChange}
              placeholder="Enter resolution"
              required
            />

            <label htmlFor="halfWidth">Half Width</label>
            <input
              type="number"
              id="halfWidth"
              name="halfWidth"
              value={form.halfWidth}
              onChange={handleChange}
              placeholder="Enter half width"
              required
            />

            <label htmlFor="austeniteContent">Austenite Content (%)</label>
            <input
              type="number"
              id="austeniteContent"
              name="austeniteContent"
              value={form.austeniteContent}
              onChange={handleChange}
              placeholder="Enter austenite content"
              required
            />

            <label htmlFor="carbonContent">Carbon Content (%)</label>
            <input
              type="number"
              id="carbonContent"
              name="carbonContent"
              value={form.carbonContent}
              onChange={handleChange}
              placeholder="Enter carbon content"
              required
            />

            <button
              type="submit"
              className="button-primary"
              style={{ marginTop: 20, width: "100%" }}
              disabled={loading}
            >
              {loading ? "Generating..." : "Analyze"}
            </button>
          </fieldset>
        </form>

        {/* Chart on the right */}
        <div
          style={{
            flex: 1,
            maxWidth: 1500,
            minHeight: 400,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            border: "1px solid #ddd",
            borderRadius: 10,
            padding: 20,
            backgroundColor: "#f9faff",
            boxShadow: "0 0 10px rgba(0, 191, 255, 0.15)",
          }}
        >
          {loading && <p style={{ fontSize: 18, color: "#007acc" }}>Generating chart...</p>}

          {imageSrc && !loading && (
            <>
              <img
                src={imageSrc}
                alt="Analysis Chart"
                style={{ maxWidth: "100%", borderRadius: 10, boxShadow: "0 0 15px #00bfff" }}
              />
              <p
                style={{
                  marginTop: 20,
                  fontWeight: "900",
                  fontSize: "1.5rem",
                  color: "#0066cc",
                  textShadow: "1px 1px 2px rgba(0,0,0,0.1)",
                  fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
                }}
              >
                X-Ray Diffraction Analysis Result
              </p>
            </>
          )}

          {!imageSrc && !loading && (
            <p style={{ color: "#aaa", fontStyle: "italic" }}>Fill in parameters and click Analyze</p>
          )}
        </div>
      </div>
    </div>
  );
}

export default App;
