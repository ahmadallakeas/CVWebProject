using CV.Data;
using CV.Models;
using CV.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CV.Pages
{
    public class SendCVModel : PageModel
    {
        public int x { get; set; }
        public int y { get; set; }
        [BindProperty]
        public int firstNumber { get; set; }
        [BindProperty]
        public int secondNumber { get; set; }
        [BindProperty]
        [Required]
        public List<string> Skills { get; set; }

        public IEnumerable<Nationality> nationalities { get; set; }

        public IEnumerable<Skill> skills { get; set; }
        public List<Skill> mylist;
        public IEnumerable<SelectListItem> listItems;
        public string[] genderList = new[] { "Male", "Female" };
        private readonly CvDbContext _db;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly Grade gradeService;

        [BindProperty]
        public InputModel InputModel { get; set; }

        public SendCVModel(CvDbContext db, IWebHostEnvironment webHostEnvironment, Grade GradeService)
        {
            this.webHostEnvironment = webHostEnvironment;
            gradeService = GradeService;
            this._db = db;
        }

        public async Task<IActionResult> OnGetAsync()
        {

            PopulateData();
            return Page();
        }
        public async Task<IActionResult> OnPostSend()
        {
            int res = 0;
            if (!ModelState.IsValid)
            {
                PopulateData();
                return Page();
            }


            if (!ValidateImage(InputModel.Image.FileName))
            {
                ModelState.AddModelError("WrongFormat", "Please upload a suitable image");
                res = -1;
            }
            if (Convert.ToInt32(InputModel.Sum) != firstNumber + secondNumber)
            {
                ModelState.AddModelError("WrongSum", "Sum is incorrect");
                res = -1;
            }
            if (InputModel.Email != InputModel.ConfirmEmail)
            {
                ModelState.AddModelError("WrongEmail", "Emails are not equal");
                res = -1;
            }
            if (DateTime.Compare(DateTime.Now, InputModel.Birthdate) < 0)
            {
                ModelState.AddModelError("WrongDate", "Please select a correct date");
                res = -1;
            }

            if (res > -1)
            {
                res = await AddCV();

            }
            if (res > 0)
            {
                return Redirect("SummaryCV?cvId="+res);
            }

            PopulateData();
            return Page();

        }
        protected void PopulateData()
        {
            Random rnd = new Random();
            x = rnd.Next(1, 20);
            y = rnd.Next(20, 50);
            nationalities = _db.Nationality.ToList();
            listItems = nationalities.OrderBy(s => s.Country)
                .Select(i => new SelectListItem
                {

                    Value = i.NationalityId.ToString(),
                    Text = i.Country
                });
            skills = _db.Skill.ToList();

        }
        protected async Task<int> AddCV()
        {

            User user = new User();
            foreach (var i in Skills)
            {
                user.Skills.Add(await _db.Skill.Where(s => s.SkillId == Convert.ToInt32(i)).FirstOrDefaultAsync());
            }
            Nationality n = await _db.Nationality.Where(i => i.NationalityId == Convert.ToInt32(InputModel.Nationality)).FirstOrDefaultAsync();
            user.Nationality = n;
            user.FirstName = InputModel.FirstName;
            user.LastName = InputModel.LastName;
            user.DateOfBirth = InputModel.Birthdate;
            user.Gender = InputModel.Gender;
            user.Email = InputModel.Email;
            user.Grade = gradeService.CaluclateGrade(user);
            user.ImagePath = ProcessUploadedFile();
            await _db.AddAsync(user);
             await _db.SaveChangesAsync();
            return user.UserId;

        }
        private string ProcessUploadedFile()
        {
            string uniqueFileName = null;

            if (InputModel.Image != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(InputModel.Image.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    InputModel.Image.CopyTo(fileStream);
                }
            }

            return uniqueFileName;
        }
        bool ValidateImage(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            switch (ext.ToLower())
            {

                case ".jpg":
                    return true;
                case ".jpeg":
                    return true;
                case ".png":
                    return true;
                default:
                    return false;
            }
        }

    }
    public class InputModel
    {
        [DisplayName("First Name")]
        [Required]
        [BindProperty]
        [MinLength(2)]
        [MaxLength(20)]
        public string FirstName { get; set; }

        [MinLength(2, ErrorMessage = "Please Enter a longer name")]
        [MaxLength(20, ErrorMessage = "Name is too long,Please enter a shorter name")]
        [DisplayName("Last Name")]
        [Required]
        [BindProperty]
        public string LastName { get; set; }

        [DisplayName("Nationality")]
        [Required]
        [BindProperty]
        public string Nationality { get; set; }

        [Required]
        [BindProperty]
        [DisplayName("Birth Date")]

        [DataType(DataType.Date)]
        public DateTime Birthdate { get; set; }
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [DisplayName("Email Address")]
        [Required]
        public string Email { get; set; }
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [DisplayName("Confirm Email Address")]
        [Required]
        public string ConfirmEmail { get; set; }
        [DisplayName("Gender")]
        [Required]
        [BindProperty]
        public string Gender { get; set; }
        [BindProperty]
        [Required]
        public IFormFile Image { get; set; }
        [BindProperty]
        [Required]
        public string Sum { get; set; }
    }
}
