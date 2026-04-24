import { Routes, Route } from "react-router-dom";
import { DashboardPage } from "./pages/DashboardPage";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<DashboardPage />} />
      <Route path="/business/:businessId" element={<DashboardPage />} />
    </Routes>
  );
}
