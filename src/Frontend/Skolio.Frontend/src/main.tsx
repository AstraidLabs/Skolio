import React from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <div className="p-6 text-slate-900">Skolio Frontend Host Shell</div>
  </React.StrictMode>
);
