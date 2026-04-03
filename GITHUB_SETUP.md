# 🚀 HƯỚNG DẪN PUSH LÊN GITHUB

## 📋 CÁC BƯỚC THỰC HIỆN

### BƯỚC 1: Tạo Repository trên GitHub
1. Đăng nhập vào GitHub (https://github.com)
2. Click **+** (trên góc phải) → **New repository**
3. Điền thông tin:
   - **Repository name**: `DreamHome3D-MB`
   - **Description**: `A mobile puzzle game where players push boxes to goal positions`
   - **Public/Private**: Chọn `Public` (để mọi người có thể xem)
   - ✅ **Tick "Add a README file"** (optional)
   - ✅ **Tick "Add .gitignore"** → Choose "Unity"
4. Click **Create repository**

### BƯỚC 2: Configure Git (Nếu chưa làm)
```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### BƯỚC 3: Add Files & Commit
Mở PowerShell/Terminal tại folder `D:\Unity\DreamHome` và chạy:

```bash
# Kiểm tra trạng thái
git status

# Thêm tất cả files (trừ những trong .gitignore)
git add .

# Commit lần đầu
git commit -m "Initial commit: Set up DreamHome puzzle game project"

# Xem log
git log
```

### BƯỚC 4: Push lên GitHub
Sau khi tạo repository trên GitHub, chạy lệnh này:

```bash
# Set remote origin (thay YOUR_USERNAME bằng username GitHub của bạn)
git remote add origin https://github.com/YOUR_USERNAME/DreamHome3D-MB.git

# Rename branch main (nếu cần)
git branch -M main

# Push code lên
git push -u origin main
```

### BƯỚC 5 (Alternative - SSH): Nếu muốn dùng SSH

Nếu bạn đã set up SSH keys, dùng lệnh này thay thế:
```bash
git remote add origin git@github.com:YOUR_USERNAME/DreamHome3D-MB.git
git branch -M main
git push -u origin main
```

---

## ✅ KIỂM TRA KẾT QUẢ

Sau khi push, vào GitHub profile bạn sẽ thấy:
- Repository `DreamHome3D-MB` 
- Tất cả files và folders
- Commit history
- README

---

## 🔧 CÁC LỆNH HỮU ÍCH

### Kiểm tra status
```bash
git status
```

### Xem commit log
```bash
git log --oneline
```

### Tạo thêm commits sau này
```bash
git add .
git commit -m "Your commit message"
git push
```

### Nếu muốn update remote origin
```bash
git remote set-url origin https://github.com/YOUR_USERNAME/DreamHome3D-MB.git
```

---

## 🚨 TROUBLESHOOT

**Lỗi: "fatal: not a git repository"**
- Chắc chắn bạn đã chạy `git init` ✅ (đã làm rồi)

**Lỗi: "Please tell me who you are"**
- Chạy: `git config --global user.name "Your Name"` và `git config --global user.email "your@email.com"`

**Lỗi: "repository not found"**
- Kiểm tra GitHub username + password
- Hoặc dùng Personal Access Token (PAT) thay vì password

**Muốn xóa commits cục bộ**
```bash
git reset --soft HEAD~1  # Giữ changes
git reset --hard HEAD~1  # Xóa hoàn toàn
```

---

## 📚 TÀI LIỆU THAM KHẢO
- GitHub Docs: https://docs.github.com/en
- Git Guide: https://git-scm.com/book/en/v2

---

**Good luck! 🎉**

