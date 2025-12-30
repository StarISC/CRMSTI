
<%@ Page Language="C#" AutoEventWireup="true" CodeFile="AgentDetail.aspx.cs" Inherits="AgentDetail" MasterPageFile="~/Site.Master" %>
<asp:Content ID="TitleContent" ContentPlaceHolderID="TitleContent" runat="server">Chi tiết đại lý</asp:Content>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style>
        .section-title { font-weight: 700; color: #1d2353; margin-bottom: 8px; }
        .info-label { font-size: 12px; color: #6b7280; }
        .info-value { font-weight: 600; }
        .tag-status { display: inline-block; padding: 2px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; }
        .tag-active { background: #e8f5e9; color: #2e7d32; border: 1px solid #c8e6c9; }
        .tag-inactive { background: #ffebee; color: #c62828; border: 1px solid #ffcdd2; }
    </style>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <input type="hidden" id="currentRole" value="<%= Session["Role"] as string %>" />
    <div class="card shadow-sm mb-3">
        <div class="card-body">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <h2 class="h4 mb-1 text-primary">Chi tiết đại lý</h2>
                    <div class="text-muted">Thông tin tổng quan và hồ sơ pháp lý</div>
                </div>
                <div class="d-flex gap-2">
                    <button type="button" id="btnEditAgent" class="btn btn-primary">Cập nhật</button>
                    <a href="Agents.aspx" class="btn btn-outline-secondary">Quay lại</a>
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm">
        <div class="card-body">
            <ul class="nav nav-tabs" id="agentTabs" role="tablist">
                <li class="nav-item" role="presentation">
                    <button class="nav-link active" id="tab-general" data-bs-toggle="tab" data-bs-target="#tabGeneral" type="button" role="tab">Thông tin chung</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="tab-accounts" data-bs-toggle="tab" data-bs-target="#tabAccounts" type="button" role="tab">Tài khoản đại lý</button>
                </li>
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="tab-docs" data-bs-toggle="tab" data-bs-target="#tabDocs" type="button" role="tab">Hồ sơ đính kèm</button>
                </li>
            </ul>
            <div class="tab-content pt-3">
                <div class="tab-pane fade show active" id="tabGeneral" role="tabpanel">
                    <div class="section-title">Thông tin đại lý</div>
                    <div class="row g-3">
                        <div class="col-md-3"><div class="info-label">Mã</div><div id="dCode" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Tên</div><div id="dName" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Loại</div><div id="dType" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Trạng thái</div><div id="dStatus" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Điện thoại</div><div id="dPhone" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Email</div><div id="dEmail" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Mã số thuế</div><div id="dTaxCode" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Địa chỉ thuế</div><div id="dTaxAddress" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Người đại diện</div><div id="dRepName" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">SĐT đại diện</div><div id="dRepPhone" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">% hoa hồng</div><div id="dCommission" class="info-value"></div></div>
                        <div class="col-md-3"><div class="info-label">Công ty chủ quản</div><div id="dParent" class="info-value"></div></div>
                        <div class="col-md-6"><div class="info-label">Địa chỉ</div><div id="dAddress" class="info-value"></div></div>
                        <div class="col-md-12"><div class="info-label">Ghi chú</div><div id="dNote" class="info-value"></div></div>
                    </div>
                </div>

                <div class="tab-pane fade" id="tabAccounts" role="tabpanel">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <div class="section-title mb-0">Danh sách tài khoản đại lý</div>
                        <button type="button" id="btnAddUser" class="btn btn-primary btn-sm">Thêm tài khoản</button>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-sm table-striped align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th>Username</th>
                                    <th>Họ tên</th>
                                    <th>Điện thoại</th>
                                    <th>Email</th>
                                    <th>Trạng thái</th>
                                    <th class="text-end">Thao tác</th>
                                </tr>
                            </thead>
                            <tbody id="userBody"></tbody>
                        </table>
                    </div>
                </div>

                <div class="tab-pane fade" id="tabDocs" role="tabpanel">
                    <div class="section-title">Hồ sơ đính kèm</div>
                    <div class="row g-3 mb-3">
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Loại hồ sơ</label>
                            <select id="docType" class="form-select">
                                <option value="CONTRACT">Hợp đồng</option>
                                <option value="LICENSE">Giấy phép</option>
                                <option value="OTHER">Khác</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Số hồ sơ</label>
                            <input type="text" id="docNo" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Ngày hồ sơ</label>
                            <input type="date" id="docDate" class="form-control" />
                        </div>
                        <div class="col-md-12">
                            <label class="form-label fw-semibold">Tải file</label>
                            <input type="file" id="docFiles" class="form-control" multiple />
                        </div>
                    </div>
                    <button type="button" class="btn btn-success" id="btnUploadDocs">Upload hồ sơ</button>
                    <div id="docError" class="text-danger mt-2" style="display:none;"></div>
                    <div class="table-responsive mt-3">
                        <table class="table table-sm table-striped align-middle mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th>Loại</th>
                                    <th>Số</th>
                                    <th>Ngày</th>
                                    <th>File</th>
                                </tr>
                            </thead>
                            <tbody id="docBody"></tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <div class="modal fade" id="agentUserModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Tài khoản đại lý</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" id="userId" />
                    <div class="mb-2">
                        <label class="form-label fw-semibold">Username</label>
                        <input type="text" id="userUsername" class="form-control" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label fw-semibold">Mật khẩu</label>
                        <input type="password" id="userPassword" class="form-control" />
                        <div class="form-text">Để trống nếu không đổi mật khẩu khi sửa.</div>
                    </div>
                    <div class="mb-2">
                        <label class="form-label fw-semibold">Họ tên</label>
                        <input type="text" id="userFullName" class="form-control" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label fw-semibold">Điện thoại</label>
                        <input type="text" id="userPhone" class="form-control" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label fw-semibold">Email</label>
                        <input type="text" id="userEmail" class="form-control" />
                    </div>
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" id="userActive" checked="checked" />
                        <label class="form-check-label" for="userActive">Hoạt động</label>
                    </div>
                    <div id="userError" class="text-danger mt-2" style="display:none;"></div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                    <button type="button" class="btn btn-primary" id="btnSaveUser">Lưu</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="agentEditModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Cập nhật đại lý</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="row g-3">
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Tên đại lý</label>
                            <input type="text" id="editName" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Loại</label>
                            <select id="editType" class="form-select">
                                <option value="COMPANY">Công ty</option>
                                <option value="COLLAB">CTV</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Trạng thái</label>
                            <select id="editStatus" class="form-select">
                                <option value="ACTIVE">Hoạt động</option>
                                <option value="INACTIVE">Tạm dừng</option>
                            </select>
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Điện thoại</label>
                            <input type="text" id="editPhone" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">Email</label>
                            <input type="text" id="editEmail" class="form-control" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label fw-semibold">% hoa hồng</label>
                            <input type="number" id="editCommission" class="form-control" min="0" step="0.01" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Mã số thuế</label>
                            <input type="text" id="editTaxCode" class="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Địa chỉ thuế</label>
                            <input type="text" id="editTaxAddress" class="form-control" />
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Tỉnh/Thành</label>
                            <select id="editProvince" class="form-select">
                                <option value="">-- Chọn tỉnh --</option>
                            </select>
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Phường/Xã</label>
                            <select id="editWard" class="form-select">
                                <option value="">-- Chọn phường --</option>
                            </select>
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Số nhà</label>
                            <input type="text" id="editHouseNo" class="form-control" />
                        </div>
                        <div class="col-12 col-md-3">
                            <label class="form-label fw-semibold">Đường</label>
                            <input type="text" id="editStreet" class="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">Người đại diện</label>
                            <input type="text" id="editRepName" class="form-control" />
                        </div>
                        <div class="col-md-6">
                            <label class="form-label fw-semibold">SĐT đại diện</label>
                            <input type="text" id="editRepPhone" class="form-control" />
                        </div>
                        <div class="col-md-12">
                            <label class="form-label fw-semibold">Ghi chú</label>
                            <textarea id="editNote" class="form-control" rows="2"></textarea>
                        </div>
                    </div>
                    <div id="editAgentError" class="text-danger mt-2" style="display:none;"></div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Đóng</button>
                    <button type="button" class="btn btn-primary" id="btnSaveAgentEdit">Lưu</button>
                </div>
            </div>
        </div>
    </div>
    <script>
        var currentAgent = null;

        $(function () {
            var agentId = getParam('id');
            if (!agentId) return;
            loadAgent(agentId);
            loadDocs(agentId);
            loadUsers(agentId);
            setupUserActions(agentId);
            setupEditAgent(agentId);

            $('#btnUploadDocs').on('click', function () {
                var files = $('#docFiles')[0].files;
                if (!files || !files.length) {
                    $('#docError').text('Vui lòng chọn file').show();
                    return;
                }
                uploadDocs(agentId, files);
            });
        });

        function getParam(name) {
            var params = new URLSearchParams(window.location.search);
            return params.get(name);
        }

        function loadAgent(agentId) {
            $.getJSON('AgentDetailApi.aspx', { id: agentId }, function (resp) {
                if (!resp || resp.error) return;
                currentAgent = resp;
                $('#dCode').text(resp.Code || '');
                $('#dName').text(resp.Name || '');
                $('#dType').text(resp.AgentType || '');
                var statusKey = (resp.Status || '').toString().toUpperCase();
                if (statusKey === 'INACTIVE') {
                    $('#dStatus').html('<span class="tag-status tag-inactive">Tạm dừng</span>');
                } else if (statusKey) {
                    $('#dStatus').html('<span class="tag-status tag-active">Hoạt động</span>');
                } else {
                    $('#dStatus').text('');
                }
                $('#dPhone').text(resp.Phone || '');
                $('#dEmail').text(resp.Email || '');
                $('#dTaxCode').text(resp.TaxCode || '');
                $('#dTaxAddress').text(resp.TaxAddress || '');
                $('#dRepName').text(resp.RepresentativeName || '');
                $('#dRepPhone').text(resp.RepresentativePhone || '');
                $('#dCommission').text(resp.CommissionRate || '');
                $('#dParent').text(resp.ParentName || '');
                $('#dNote').text(resp.Note || '');
                var addressText = resp.FullAddress || buildAddressLine(resp);
                $('#dAddress').text(addressText);
            });
        }

        function loadDocs(agentId) {
            $.getJSON('AgentDocumentsApi.aspx', { id: agentId }, function (resp) {
                if (!resp || resp.error) return;
                var rows = '';
                if (resp.data && resp.data.length) {
                    $.each(resp.data, function (i, d) {
                        var file = d.FilePath ? '<a href="' + d.FilePath + '" target="_blank">' + d.FileName + '</a>' : '';
                        rows += '<tr>' +
                            '<td>' + (d.DocType || '') + '</td>' +
                            '<td>' + (d.DocNo || '') + '</td>' +
                            '<td>' + (d.DocDate || '') + '</td>' +
                            '<td>' + file + '</td>' +
                            '</tr>';
                    });
                } else {
                    rows = '<tr><td colspan="4" class="text-muted text-center">Chưa có hồ sơ</td></tr>';
                }
                $('#docBody').html(rows);
            });
        }

        function loadUsers(agentId) {
            $.getJSON('AgentUsersApi.aspx', { id: agentId }, function (resp) {
                if (!resp || resp.error) return;
                var rows = '';
                if (resp.data && resp.data.length) {
                    $.each(resp.data, function (i, u) {
                        var actions = '';
                        if (isAdmin()) {
                            actions = '<div class="d-flex justify-content-end gap-2">' +
                                '<button type="button" class="btn btn-sm btn-outline-primary btn-edit-user" data-id="' + u.Id + '">Sửa</button>' +
                                '<button type="button" class="btn btn-sm btn-outline-danger btn-delete-user" data-id="' + u.Id + '">Xóa</button>' +
                                '</div>';
                        }
                        rows += '<tr>' +
                            '<td>' + (u.Username || '') + '</td>' +
                            '<td>' + (u.FullName || '') + '</td>' +
                            '<td>' + (u.Phone || '') + '</td>' +
                            '<td>' + (u.Email || '') + '</td>' +
                            '<td>' + (u.IsActive
                                ? '<span class="tag-status tag-active">Hoạt động</span>'
                                : '<span class="tag-status tag-inactive">Tạm dừng</span>') + '</td>' +
                            '<td class="text-end">' + actions + '</td>' +
                            '</tr>';
                    });
                } else {
                    rows = '<tr><td colspan="6" class="text-muted text-center">Chưa có tài khoản</td></tr>';
                }
                $('#userBody').html(rows);
            });
        }

        function setupUserActions(agentId) {
            if (!isAdmin()) {
                $('#btnAddUser').hide();
                return;
            }

            $('#btnAddUser').on('click', function (e) {
                e.preventDefault();
                openUserModal();
            });

            $('#btnSaveUser').on('click', function (e) {
                e.preventDefault();
                saveUser(agentId);
            });

            $('#userBody').on('click', '.btn-edit-user', function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                editUser(id);
            });

            $('#userBody').on('click', '.btn-delete-user', function (e) {
                e.preventDefault();
                var id = $(this).data('id');
                if (!confirm('Bạn có chắc muốn xóa tài khoản này?')) return;
                deleteUser(agentId, id);
            });
        }
        function setupEditAgent(agentId) {
            if (!isAdmin()) {
                $('#btnEditAgent').hide();
                return;
            }
            $('#btnEditAgent').on('click', function (e) {
                e.preventDefault();
                openEditAgentModal();
            });

            $('#btnSaveAgentEdit').on('click', function (e) {
                e.preventDefault();
                saveAgentEdit(agentId);
            });
        }

        function isAdmin() {
            return ($('#currentRole').val() || '').toLowerCase() === 'admin';
        }

        function openEditAgentModal() {
            if (!currentAgent) return;
            $('#editName').val(currentAgent.Name || '');
            $('#editType').val(currentAgent.AgentType || 'COMPANY');
            $('#editStatus').val(currentAgent.Status || 'ACTIVE');
            $('#editPhone').val(currentAgent.Phone || '');
            $('#editEmail').val(currentAgent.Email || '');
            $('#editTaxCode').val(currentAgent.TaxCode || '');
            $('#editTaxAddress').val(currentAgent.TaxAddress || '');
            $('#editCommission').val((currentAgent.CommissionRate || '').toString().replace('%', '').trim());
            $('#editRepName').val(currentAgent.RepresentativeName || '');
            $('#editRepPhone').val(currentAgent.RepresentativePhone || '');
            $('#editNote').val(currentAgent.Note || '');
            $('#editHouseNo').val(currentAgent.HouseNo || '');
            $('#editStreet').val(currentAgent.Street || '');
            loadProvinces('#editProvince', '#editWard', currentAgent.ProvinceId, currentAgent.WardId);
            $('#editAgentError').hide().text('');
            var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('agentEditModal'));
            modal.show();
        }

        function saveAgentEdit(agentId) {
            var payload = {
                id: agentId,
                name: $('#editName').val(),
                type: $('#editType').val(),
                status: $('#editStatus').val(),
                phone: $('#editPhone').val(),
                email: $('#editEmail').val(),
                taxCode: $('#editTaxCode').val(),
                taxAddress: $('#editTaxAddress').val(),
                provinceId: $('#editProvince').val(),
                wardId: $('#editWard').val(),
                houseNo: $('#editHouseNo').val(),
                street: $('#editStreet').val(),
                fullAddress: buildAddressText('#editHouseNo', '#editStreet', '#editWard', '#editProvince'),
                commissionRate: $('#editCommission').val(),
                representativeName: $('#editRepName').val(),
                representativePhone: $('#editRepPhone').val(),
                contractNo: '',
                contractDate: '',
                contractExpiry: '',
                licenseNo: '',
                licenseDate: '',
                licenseExpiry: '',
                note: $('#editNote').val()
            };

            $.ajax({
                url: 'AgentUpdateApi.aspx',
                type: 'POST',
                dataType: 'json',
                data: payload,
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#editAgentError').text(resp.error).show();
                        return;
                    }
                    bootstrap.Modal.getOrCreateInstance(document.getElementById('agentEditModal')).hide();
                    loadAgent(agentId);
                },
                error: function () {
                    $('#editAgentError').text('Lỗi cập nhật đại lý').show();
                }
            });
        }

        function loadProvinces(provinceSelector, wardSelector, selectedProvince, selectedWard) {
            $.ajax({
                url: 'ProvincesApi.aspx',
                type: 'GET',
                dataType: 'json',
                success: function (resp) {
                    var $province = $(provinceSelector);
                    $province.empty();
                    $province.append('<option value="">-- Chọn tỉnh --</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, p) {
                            var selected = selectedProvince && p.Id == selectedProvince ? ' selected' : '';
                            $province.append('<option value="' + p.Id + '"' + selected + '>' + p.Name + '</option>');
                        });
                    }
                    loadWards(wardSelector, $province.val(), selectedWard);
                }
            });

            $(document).off('change', provinceSelector).on('change', provinceSelector, function () {
                loadWards(wardSelector, $(this).val(), null);
            });
        }

        function loadWards(wardSelector, provinceId, selectedWard) {
            $.ajax({
                url: 'WardsApi.aspx',
                type: 'GET',
                dataType: 'json',
                data: { provinceId: provinceId || '' },
                success: function (resp) {
                    var $ward = $(wardSelector);
                    $ward.empty();
                    $ward.append('<option value="">-- Chọn phường --</option>');
                    if (resp && resp.data && resp.data.length) {
                        $.each(resp.data, function (i, w) {
                            var selected = selectedWard && w.Id == selectedWard ? ' selected' : '';
                            $ward.append('<option value="' + w.Id + '"' + selected + '>' + w.Name + '</option>');
                        });
                    }
                }
            });
        }

        function buildAddressText(houseSelector, streetSelector, wardSelector, provinceSelector) {
            var parts = [];
            var house = $(houseSelector).val();
            var street = $(streetSelector).val();
            var ward = $(wardSelector + ' option:selected').text();
            var province = $(provinceSelector + ' option:selected').text();
            if (house) parts.push(house);
            if (street) parts.push(street);
            if (ward && ward.indexOf('Chọn') === -1) parts.push(ward);
            if (province && province.indexOf('Chọn') === -1) parts.push(province);
            return parts.join(', ');
        }

        function buildAddressLine(resp) {
            if (!resp) return '';
            var parts = [];
            if (resp.HouseNo) parts.push(resp.HouseNo);
            if (resp.Street) parts.push(resp.Street);
            if (resp.WardName) parts.push(resp.WardName);
            if (resp.ProvinceName) parts.push(resp.ProvinceName);
            return parts.join(', ');
        }

        function openUserModal(user) {
            $('#userId').val(user ? user.Id : '');
            $('#userUsername').val(user ? user.Username : '');
            $('#userPassword').val('');
            $('#userFullName').val(user ? user.FullName : '');
            $('#userPhone').val(user ? user.Phone : '');
            $('#userEmail').val(user ? user.Email : '');
            $('#userActive').prop('checked', user ? !!user.IsActive : true);
            $('#userError').hide().text('');
            var modal = new bootstrap.Modal(document.getElementById('agentUserModal'));
            modal.show();
        }

        function editUser(id) {
            $.getJSON('AgentUsersApi.aspx', { id: getParam('id') }, function (resp) {
                if (!resp || resp.error || !resp.data) return;
                var user = null;
                $.each(resp.data, function (i, u) {
                    if (u.Id == id) {
                        user = u;
                        return false;
                    }
                });
                if (user) {
                    openUserModal(user);
                }
            });
        }

        function saveUser(agentId) {
            var payload = {
                id: $('#userId').val(),
                agentId: agentId,
                username: $('#userUsername').val(),
                password: $('#userPassword').val(),
                fullName: $('#userFullName').val(),
                phone: $('#userPhone').val(),
                email: $('#userEmail').val(),
                isActive: $('#userActive').is(':checked') ? '1' : '0'
            };

            $.ajax({
                url: 'AgentUsersSaveApi.aspx',
                type: 'POST',
                data: payload,
                success: function (resp) {
                    if (resp && resp.error) {
                        $('#userError').text(resp.error).show();
                        return;
                    }
                    bootstrap.Modal.getInstance(document.getElementById('agentUserModal')).hide();
                    loadUsers(agentId);
                },
                error: function () {
                    $('#userError').text('Lỗi lưu tài khoản').show();
                }
            });
        }

        function deleteUser(agentId, id) {
            $.ajax({
                url: 'AgentUsersDeleteApi.aspx',
                type: 'POST',
                data: { id: id, agentId: agentId },
                success: function (resp) {
                    if (resp && resp.error) {
                        alert(resp.error);
                        return;
                    }
                    loadUsers(agentId);
                },
                error: function () {
                    alert('Lỗi xóa tài khoản');
                }
            });
        }

        function uploadDocs(agentId, files) {
            var formData = new FormData();
            formData.append('agentId', agentId);
            formData.append('docType', $('#docType').val());
            formData.append('docNo', $('#docNo').val());
            formData.append('docDate', $('#docDate').val());
            for (var i = 0; i < files.length; i++) {
                formData.append('files', files[i]);
            }
            $.ajax({
                url: 'AgentsUploadApi.aspx',
                type: 'POST',
                data: formData,
                contentType: false,
                processData: false,
                success: function () {
                    $('#docError').hide().text('');
                    $('#docFiles').val('');
                    loadDocs(agentId);
                },
                error: function () {
                    $('#docError').text('Lỗi upload hồ sơ').show();
                }
            });
        }
    </script>
</asp:Content>
