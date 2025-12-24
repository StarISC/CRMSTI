<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Login.aspx.cs" Inherits="Login" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Đăng nhập CRM</title>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link rel="icon" href="document/StarTravel.ico" type="image/x-icon" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" />
    <style>
        body {
            margin: 0; padding: 0;
            font-family: 'Segoe UI', 'Inter', system-ui, -apple-system, sans-serif;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            color: #f4f6fb;
            background: radial-gradient(circle at 10% 20%, #1e4072 0, #1d2353 25%, #0f172a 60%);
            position: relative;
            overflow: hidden;
        }
        .orb {
            position: absolute;
            filter: blur(60px);
            opacity: 0.3;
        }
        .orb.one { width: 280px; height: 280px; background: #4da3ff; top: -60px; left: -40px; }
        .orb.two { width: 340px; height: 340px; background: #8a63ff; bottom: -80px; right: -60px; }
        .login-panel {
            position: relative;
            width: 100%;
            max-width: 440px;
            background: #fff;
            border-radius: 16px;
            border: 1px solid #e3e8f5;
            box-shadow: 0 20px 60px rgba(15, 46, 101, 0.15);
            padding: 32px 30px 36px;
        }
        .brand {
            display: flex; align-items: center; justify-content: center; gap: 12px;
            margin-bottom: 16px;
        }
        .brand img { height: 48px; }
        .brand .text { font-weight: 800; font-size: 20px; letter-spacing: .6px; color: #1d2353; }
        h2 { color: #1d2353; text-align: center; margin-bottom: 8px; font-weight: 800; }
        .subtitle { text-align: center; color: #4a5574; margin-bottom: 20px; }
        label { color: #1d2353; font-weight: 700; margin-bottom: 6px; }
        .form-control {
            background: #f6f8ff;
            border: 1px solid #d4dcf4;
            color: #0f1b2d;
        }
        .form-control:focus {
            border-color: #4a7bff;
            box-shadow: 0 0 0 3px rgba(74,123,255,0.18);
            background: #fff;
        }
        .btn-primary {
            background: linear-gradient(120deg, #4a7bff, #7e6df2);
            border: none;
            font-weight: 800;
            letter-spacing: .2px;
            box-shadow: 0 10px 30px rgba(74,123,255,0.28);
        }
        .btn-primary:hover { background: linear-gradient(120deg, #3b6ae3, #6d5cdd); }
        .message { min-height: 22px; color: #c62828; text-align: center; font-weight: 700; margin-top: 8px; }
    </style>
</head>
<body>
    <div class="orb one"></div>
    <div class="orb two"></div>
    <form id="form1" runat="server" class="w-100 px-3" style="position: relative; z-index: 2;">
        <div class="login-panel mx-auto">
            <div class="brand">
                <img src="document/logo.png" alt="StarTravel" />
                <div class="text">STAR CRM</div>
            </div>
            <h2>Đăng nhập</h2>
            <div class="subtitle">Quản trị chăm sóc khách hàng & booking</div>
            <div class="mb-3">
                <label for="txtUsername" class="form-label">Tài khoản</label>
                <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" />
            </div>
            <div class="mb-3">
                <label for="txtPassword" class="form-label">Mật khẩu</label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-control" />
            </div>
            <div class="form-check mb-3">
                <asp:CheckBox ID="chkRemember" runat="server" CssClass="form-check-input" />
                <label class="form-check-label" for="chkRemember">Ghi nhớ đăng nhập</label>
            </div>
            <asp:Button ID="btnLogin" runat="server" Text="Đăng nhập" CssClass="btn btn-primary w-100 fw-bold py-2" OnClick="btnLogin_Click" />
            <div class="message">
                <asp:Literal ID="litMessage" runat="server" />
            </div>
        </div>
    </form>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
