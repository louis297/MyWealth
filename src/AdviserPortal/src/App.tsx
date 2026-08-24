import { Outlet } from 'react-router-dom'

function App() {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* sidebar and navbar here */}
      <main>
        <Outlet />
      </main>
    </div>
  )
}

export default App