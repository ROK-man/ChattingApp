using System.ComponentModel.DataAnnotations;

namespace Website.ViewModels
{
    public class SignupViewModel
    {
        [Required(ErrorMessage = "아이디를 입력하세요.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "아이디는 5~20자 사이여야 합니다.")]
        public string UserID { get; set; }

        [Required(ErrorMessage = "비밀번호를 입력하세요.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "비밀번호는 최소 6자 이상이어야 합니다.")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "비밀번호가 일치하지 않습니다.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "닉네임을 입력하세요.")]
        [MaxLength(15, ErrorMessage = "닉네임은 15자를 초과할 수 없습니다.")]
        [RegularExpression(@"^[a-zA-Z0-9 ]+$", ErrorMessage = "영어, 숫자, 스페이스만 가능합니다.")]
        public string Nickname { get; set; }
    }
}