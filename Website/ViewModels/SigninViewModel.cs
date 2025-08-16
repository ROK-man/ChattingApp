using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Website.ViewModels
{
    public class SigninViewModel
    {
        [Required(ErrorMessage = "아이디를 입력하세요.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "아이디는 5~20자 사이여야 합니다.")]
        public string UserID { get; set; }

        [Required(ErrorMessage = "비밀번호를 입력하세요")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}