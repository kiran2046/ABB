# ABB

## 📌 Overview
**ABB** is a full-stack project that integrates:
- A **frontend** for user interaction  
- A **backend API** for managing business logic and data  
- An **ML service** for intelligent predictions/analytics  

This setup allows seamless interaction between users, the server, and machine learning models.

---

## 🚀 Features
- 🖥️ **Frontend** – Built with modern UI frameworks for a responsive experience  
- ⚙️ **Backend** – RESTful API with authentication and database integration  
- 🤖 **ML Service** – Machine learning pipeline for predictions and analytics  
- 🐳 **Docker Support** – Run the whole system easily with `docker-compose`  

---

## 🏗️ Project Structure
ABB/
│── backend/ # API and server-side logic
│── frontend/ # Client-side application
│── ml-service/ # Machine learning models & APIs
│── docker-compose.yml
│── README.md


---
## 📦 Installation & Setup

### 1. Clone the repository
```bash
git clone https://github.com/kiran2046/ABB.git
cd ABB

#### 2. Using Docker (Recommended)
docker-compose up --build

Now the app will be available at:

Frontend → http://localhost:3000

Backend API → http://localhost:5000

ML Service → http://localhost:8000
