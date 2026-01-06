# SWP391_SP26
# Hướng dẫn convention trong viết commit
**Luồng 1: Khi làm việc với Issue/Merge Request từ GitLab**

Áp dụng khi bạn bắt đầu công việc từ một Issue đã có trên GitLab và sử dụng tính năng tạo nhánh tự động.

**1. Tên Nhánh (Branch)**

**Giữ nguyên** tên nhánh do GitLab tự động tạo ra.
**Ví dụ:** 
- Issue tên: "refactor: refine flow assign and request for parent"
- Tên nhánh được tạo: "17-refactor-refine-flow-assign-and-request-for-parent"

**2. Commit Message**

Sử dụng #'Số-nhánh' làm nội dung commit message. Điều này giúp liên kết trực tiếp và tuyệt đối giữa commit và công việc trên GitLab.

**Ví dụ:**

        git commit -m "#17 refine flow assign done, api tested"

**Luồng 2: Khi thực hiện các thay đổi nhỏ (không có Issue)**

Áp dụng cho các công việc nhỏ, tự phát sinh mà không có Issue trên GitLab. Ví dụ: sửa một lỗi vặt, dọn dẹp code, cập nhật tài liệu, v.v.

**1. Tên Nhánh (Branch)**

Tự tạo issue/nhánh theo cấu trúc đơn giản: type/short-description

**Ví dụ:**

        fix/hotfix-login-button

        chore/update-readme

**2. Commit Message**

Sử dụng chuẩn **Conventional Commits** với cấu trúc type: subject.

**feat:** Cho tính năng mới (feature).

        feat: Add remember me checkbox to login form

**fix:** Cho việc sửa lỗi.

        fix: Correct button color on login page

**refactor:** Cho việc tái cấu trúc code mà không thay đổi tính năng.

        refactor: Simplify logic in user service

**docs:** Cho việc cập nhật tài liệu.

        docs: Update project workflow guide

**chore:** Cho các công việc lặt vặt không liên quan đến code.

        chore: Add editorconfig file        


# Hướng dẫn quá trình tạo branch và push code
Sơ đồ luồng công việc cơ bản:

**Nhận Task**: Một issue được tạo và gán cho bạn trên GitLab/tự tạo issue trong Issue boards.

**Tạo Branch**: Bạn tạo một nhánh mới từ issue này để bắt đầu phát triển.

**Phát triển**: Viết code, commit và đẩy (push) lên remote.

        git add .
        git commit -m "#(branchnumber) (type: description)"
        Ví dụ: git commit -m "#90 feat: add public get"

**Tạo Merge Request (MR)**: Tạo một MR để sáp nhập code của bạn vào nhánh chính (main hoặc develop).

**Review & Merge**: Code được review và sáp nhập.

**Cập nhật Local**: Đồng bộ lại nhánh chính ở local của bạn.

        git checkout develop
        git fetch
        git pull
        git checkout (branch local cần đồng bộ)
        git merge develop
        
**Bước 1: Nhận Task và Tạo Branch Mới**
Khi bạn được gán một task (issue) trên GitLab, cách tốt nhất là tạo branch trực tiếp từ giao diện của issue đó.

1. Mở issue đã được gán cho bạn.
2. Tìm và nhấp vào nút "Create merge request" hoặc "**Create branch**".
3. GitLab sẽ tự động đề xuất một tên nhánh dựa trên số ID và tiêu đề của issue (ví dụ: 1-ten-issue-cuc-ngan-gon). Bạn có thể giữ nguyên hoặc chỉnh sửa lại cho phù hợp với quy ước của team (ví dụ: feature/1-ten-issue).
4. Chọn nhánh nguồn (Source branch), là **develop**.
5. Nhấn "**Create branch**".

Nhánh của bạn giờ đã được tạo trên remote repository của GitLab.

**Bước 2: Lấy Branch Về Local và Bắt Đầu Làm Việc**

Sau khi tạo branch trên GitLab, bạn cần lấy nó về máy tính local để bắt đầu code.

1. Lấy thông tin mới nhất từ remote
Mở terminal trong thư mục dự án của bạn và chạy lệnh git fetch. Lệnh này sẽ tải về tất cả các tham chiếu mới (nhánh, tag) từ remote repository mà không tự động sáp nhập vào code hiện tại của bạn.

        git fetch

2. Chuyển sang nhánh mới

Sử dụng lệnh checkout để chuyển sang nhánh bạn vừa tạo.

        git checkout <ten-branch>
**Ví dụ:**

        git checkout origin/1-ten-issue-cuc-ngan-gon

Lợi ích của việc tracking: Khi nhánh local của bạn theo dõi nhánh remote, các lệnh như git pull và git push sẽ tự động biết cần tương tác với nhánh nào trên remote mà không cần bạn chỉ định thêm.

3. Làm việc và Commit

Bây giờ bạn đã ở trên nhánh mới. Hãy bắt đầu viết code!

- Sau khi thay đổi, hãy thêm file và commit như bình thường.
- Luôn viết commit message rõ ràng, ngắn gọn.


        git add .
        git commit -m "feat: Thêm chức năng đăng nhập cho người dùng"

4. Đẩy code lên Remote

Sau khi đã có một hoặc nhiều commit, hãy đẩy code của bạn lên GitLab. Vì bạn đã thiết lập tracking ở bước 2, bạn chỉ cần chạy:

    git push

**Bước 3: Tạo Merge Request (MR)**

Khi bạn đã hoàn thành tính năng hoặc muốn code của mình được review, hãy tạo một Merge Request.

1. Sau khi git push lần đầu tiên, Git thường sẽ cung cấp một đường link trực tiếp trong terminal để bạn tạo MR. Chỉ cần copy và dán vào trình duyệt.

2. Hoặc, bạn có thể vào trang GitLab của dự án, bạn sẽ thấy một thông báo đề xuất tạo MR cho nhánh bạn vừa đẩy lên.

3. Khi tạo MR:

**Tiêu đề**: Viết tiêu đề rõ ràng (thường là tên tính năng).

**Mô tả**: Mô tả chi tiết những thay đổi bạn đã thực hiện. Nếu MR này giải quyết một issue, hãy ghi Closes #ID_issue (ví dụ: Closes #1) trong phần mô tả để GitLab tự động đóng issue đó khi MR được merge.

**Reviewers**: Gán người review code cho bạn.

Nhấn "**Create merge request**".

**Bước 4: Quy Trình Review và Cập Nhật Local Sau Khi Merge**
1. Review Code: Reviewer sẽ xem code, để lại bình luận và có thể yêu cầu bạn chỉnh sửa.

2. Cập nhật MR: Nếu có yêu cầu thay đổi, bạn chỉ cần commit và push thêm lên nhánh của mình. Merge Request sẽ tự động được cập nhật.

3. Merge: Sau khi được chấp thuận, MR sẽ được merge vào nhánh đích (main hoặc develop).

4. Dọn dẹp và cập nhật local:
Sau khi MR đã được merge, bạn nên quay về nhánh chính và cập nhật nó.


**Chuyển về nhánh develop**

    git checkout develop

Kéo phiên bản mới nhất của nhánh develop từ remote (bao gồm cả code của bạn vừa được merge)

    git pull origin develop