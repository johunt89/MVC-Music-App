using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MVC_Music.Data;
using MVC_Music.Models;
using MVC_Music.Utilities;
using MVC_Music.ViewModels;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Microsoft.AspNetCore.Authorization;

namespace MVC_Music.Controllers
{
    [Authorize]
    public class MusiciansController : CustomControllers.ElephantController
    {
        private readonly MusicContext _context;

        public MusiciansController(MusicContext context)
        {
            _context = context;
        }

        // GET: Musicians
        public async Task<IActionResult> Index(string sortDirectionCheck, string sortFieldID, string SearchName, string SearchPhone, int? InstrumentID, int? OtherInstrumentID,
            int? page, int? pageSizeID, string actionButton, string sortDirection = "asc", string sortField = "Musician")
        {
            //Clear the sort/filter/paging URL Cookie for Controller
            CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            PopulateDropDownLists();
            ViewData["OtherInstrumentID"] = ViewData["InstrumentID"];

            //Toggle the Open/Closed state of the collapse depending on if we are filtering
            ViewData["Filtering"] = "btn-outline-primary"; 
            

            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Musician", "Phone", "Age", "Primary Instrument" };

            var musicians = _context.Musicians
                .Include(m => m.Instrument)
                .Include(d => d.MusicianDocuments)
                .Include(m => m.MusicianThumbnail)
                .Include(m=>m.Plays).ThenInclude(p => p.Instrument)
                .AsNoTracking();

            //Add as many filters as needed
            if (InstrumentID.HasValue)
            {
                musicians = musicians.Where(p => p.InstrumentID == InstrumentID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (OtherInstrumentID.HasValue)
            {
                musicians = musicians.Where(p => p.Plays.Any(p=>p.InstrumentID == OtherInstrumentID));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchName))
            {
                musicians = musicians.Where(p => p.LastName.ToUpper().Contains(SearchName.ToUpper())
                                       || p.FirstName.ToUpper().Contains(SearchName.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchPhone))
            {
                musicians = musicians.Where(p => p.Phone.ToUpper().Contains(SearchPhone.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }
            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;//Reset page to start

                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
                else
                {
                    sortDirection = String.IsNullOrEmpty(sortDirectionCheck) ? "asc" : "desc";
                    sortField = sortFieldID;
                }
            }

            //Now we know which field and direction to sort by
            if (sortField == "Phone")
            {
                if (sortDirection == "asc")
                {
                    musicians = musicians
                        .OrderBy(p => p.Phone)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(p => p.Phone)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
            }
            else if (sortField == "Age")
            {
                if (sortDirection == "asc")
                {
                    musicians = musicians
                        .OrderByDescending(p => p.DOB)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    musicians = musicians
                        .OrderBy(p => p.DOB)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
            }
            else if (sortField == "Primary Instrument")
            {
                if (sortDirection == "asc")
                {
                    musicians = musicians
                        .OrderBy(p => p.Instrument.Name)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(p => p.Instrument.Name)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
            }
            else //Sorting by Musician Name
            {
                if (sortDirection == "asc")
                {
                    musicians = musicians
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(p => p.LastName)
                        .ThenByDescending(p => p.FirstName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            ViewBag.sortFieldID = new SelectList(sortOptions, sortField.ToString());

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "musicians");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Musician>.CreateAsync(musicians.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Musicians/Details/5
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Musicians == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(m => m.MusicianDocuments)
                .Include(m => m.MusicianPhoto)
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musician == null)
            {
                return NotFound();
            }

            return View(musician);
        }


        // GET: Musicians/Create
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public IActionResult Create()
        {
            var musician = new Musician();
            PopulateAssignedPlaysData(musician);
            PopulateDropDownLists();
            return View();
        }

        // POST: Musicians/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Create([Bind("FirstName,MiddleName,LastName,Phone,DOB,SIN,InstrumentID")] Musician musician, string[] selectedOptions, IFormFile thePicture, List<IFormFile> theFiles)
        {
            try
            {
                //Add the selected plays
                if (selectedOptions != null)
                {
                    foreach (var play in selectedOptions)
                    {
                        var playToAdd = new Play { MusicianID = musician.ID, InstrumentID = int.Parse(play) };
                        musician.Plays.Add(playToAdd);
                    }
                }
                if (ModelState.IsValid)
                {
                    await AddPicture(musician, thePicture);
                    await AddDocumentsAsync(musician, theFiles);
                    _context.Add(musician);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { musician.ID });
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                {
                    ModelState.AddModelError("SIN", "Unable to save changes. Remember, you cannot have duplicate SIN numbers.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            PopulateAssignedPlaysData(musician);
            PopulateDropDownLists(musician);
            return View(musician);
        }

        // GET: Musicians/Edit/5
        [Authorize(Roles = "Staff, Supervisor, Admin")] //need to adjust for staff
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Musicians == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(d => d.MusicianDocuments)
                .Include(m => m.MusicianPhoto)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .FirstOrDefaultAsync(m => m.ID == id);
            
            if (musician == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Staff"))
            {
                if(musician.CreatedBy != User.Identity.Name)
                {
                    ModelState.AddModelError("", "Staff can only edit musicians they entered");
                    ViewData["NoSubmit"] = "disabled=disabled";
                }
            }

            PopulateAssignedPlaysData(musician);
            PopulateDropDownLists(musician);
            return View(musician);
        }

        // POST: Musicians/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Staff, Supervisor, Admin")] //need to adjust for staff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedOptions, Byte[] RowVersion, string chkRemoveImage, IFormFile thePicture, List<IFormFile> theFiles)
        {
            var musicianToUpdate = await _context.Musicians
                .Include(d => d.MusicianDocuments)
                .Include(m => m.MusicianPhoto)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (User.IsInRole("Staff"))
            {
                if (musicianToUpdate.CreatedBy != User.Identity.Name)
                {
                    ModelState.AddModelError("", "Staff can only edit musicians they entered");
                    ViewData["NoSubmit"] = "disabled=disabled";
                    return View(musicianToUpdate);
                }
            }

            //Update the plays
            UpdatePlays(selectedOptions, musicianToUpdate);

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(musicianToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Musician>(musicianToUpdate, "",
                p => p.SIN, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
                p => p.Phone, p => p.InstrumentID))
            {
                try
                {
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        musicianToUpdate.MusicianThumbnail = _context.MusicianThumbnails.Where(m => m.MusicianID == musicianToUpdate.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        musicianToUpdate.MusicianPhoto = null;
                        musicianToUpdate.MusicianThumbnail = null;
                    }
                    else
                    {
                        await AddDocumentsAsync(musicianToUpdate, theFiles);
                        await AddPicture(musicianToUpdate, thePicture);
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { musicianToUpdate.ID });
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Musician)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Musician was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Musician)databaseEntry.ToObject();
                        if (databaseValues.FirstName != clientValues.FirstName)
                            ModelState.AddModelError("FirstName", "Current value: "
                                + databaseValues.FirstName);
                        if (databaseValues.MiddleName != clientValues.MiddleName)
                            ModelState.AddModelError("MiddleName", "Current value: "
                                + databaseValues.MiddleName);
                        if (databaseValues.LastName != clientValues.LastName)
                            ModelState.AddModelError("LastName", "Current value: "
                                + databaseValues.LastName);
                        if (databaseValues.SIN != clientValues.SIN)
                            ModelState.AddModelError("SIN", "Current value: "
                                + databaseValues.SINFormatted);
                        if (databaseValues.DOB != clientValues.DOB)
                            ModelState.AddModelError("DOB", "Current value: "
                                + String.Format("{0:d}", databaseValues.DOB));
                        if (databaseValues.Phone != clientValues.Phone)
                            ModelState.AddModelError("Phone", "Current value: "
                                + databaseValues.PhoneFormatted);
                        //For the foreign key, we need to go to the database to get the information to show
                        if (databaseValues.InstrumentID != clientValues.InstrumentID)
                        {
                            Instrument databaseInstrument = await _context.Instruments.FirstOrDefaultAsync(i => i.ID == databaseValues.InstrumentID);
                            ModelState.AddModelError("InstrumentID", $"Current value: {databaseInstrument?.Name}");
                        }
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to Musician List' hyperlink.");
                        musicianToUpdate.RowVersion = (byte[])databaseValues.RowVersion;
                        ModelState.Remove("RowVersion");
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed"))
                    {
                        ModelState.AddModelError("SIN", "Unable to save changes. Remember, you cannot have duplicate SIN numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }
            PopulateAssignedPlaysData(musicianToUpdate);
            PopulateDropDownLists(musicianToUpdate);
            return View(musicianToUpdate);
        }

        // GET: Musicians/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Musicians == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musician == null)
            {
                return NotFound();
            }

            return View(musician);
        }

        // POST: Musicians/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Musicians == null)
            {
                return Problem("Entity set 'MusicContext.Musicians'  is null.");
            }
            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .FirstOrDefaultAsync(m => m.ID == id);
            try
            {
                if (musician != null)
                {
                    _context.Musicians.Remove(musician);
                }
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists see your system administrator.");
            }
            return View(musician);
        }
        public async Task<FileContentResult> Download(int id)
        {
            var theFile = await _context.UploadedFiles
                .Include(m => m.FileContent)
                .Where(f => f.ID == id)
                .FirstOrDefaultAsync();
            return File(theFile.FileContent.Content, theFile.MimeType, theFile.FileName);
        }

        public IActionResult PerformanceSummary()
        {
            var summary = _context.Performances.Include(m => m.Musician)
                .GroupBy(m => new { m.MusicianID, m.Musician.FirstName, m.Musician.MiddleName, m.Musician.LastName })
                .Select(grp => new PerformanceSummaryVM
                {
                    ID = grp.Key.MusicianID,
                    FirstName = grp.Key.FirstName,
                    MiddleName = grp.Key.MiddleName,
                    LastName = grp.Key.LastName,
                    AverageFeePaid = grp.Sum(f => f.FeePaid),
                    HighestFeePaid = grp.Max(f => f.FeePaid),
                    LowestFeePaid = grp.Min(f => f.FeePaid),
                    TotalPerformances = grp.Count()
                }).OrderBy(m => m.LastName).ThenBy(m => m.FirstName);
            return View(summary.AsNoTracking().ToList());
        }

        public IActionResult DownloadPerformances()
        {
            var performances = _context.Performances.Include(m => m.Musician)
                .GroupBy(m => new { m.MusicianID, m.Musician.LastName, m.Musician.FirstName, m.Musician.MiddleName })
                .Select(m => new PerformanceSummaryVM
                {
                    FirstName = m.Key.FirstName,
                    LastName = m.Key.LastName,
                    MiddleName = m.Key.MiddleName,
                    AverageFeePaid = m.Sum(f => f.FeePaid),
                    HighestFeePaid=m.Max(f => f.FeePaid),
                    LowestFeePaid=m.Min(f => f.FeePaid),
                    TotalPerformances = m.Count()
                }).OrderBy(m => m.LastName).ThenBy(m => m.FirstName);

            int numRows = performances.Count();
            if (numRows  > 0)
            {
                using(ExcelPackage excel =new ExcelPackage())
                {
                    DateTime utcDate = DateTime.UtcNow;
                    TimeZoneInfo esTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    DateTime localDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, esTimeZone);

                    var workSheet = excel.Workbook.Worksheets.Add("Performances" + localDate);

                    workSheet.Cells[3, 1].LoadFromCollection(performances.Select(x => new {x.FormalName, x.AverageFeePaid, x.HighestFeePaid, x.LowestFeePaid, x.TotalPerformances}), true);
                    workSheet.Cells[5,1,7,1].Style.Numberformat.Format = "###,##0.00";

                    using (ExcelRange totalPerformers = workSheet.Cells[numRows + 4, 6])
                    {
                        totalPerformers.Formula = "Sum(" + workSheet.Cells[4, 6].Address + ":" + workSheet.Cells[numRows + 3, 6].Address + ")";
                        totalPerformers.Style.Font.Bold = true;
                        totalPerformers.Style.Numberformat.Format = "###,##0.00";
                    }

                    using (ExcelRange totalMusicians = workSheet.Cells[numRows + 4, 2])
                    {
                        totalMusicians.Formula = "CountA(" + workSheet.Cells[4, 2].Address + ":" + workSheet.Cells[numRows + 3, 2].Address + ")";
                        totalMusicians.Style.Font.Bold = true;
                        totalMusicians.Style.Numberformat.Format = "###,##0.00";
                    }

                    workSheet.Cells[1, 1].Value = "Performances Report";
                    using (ExcelRange rng = workSheet.Cells[1,1,1,6])
                    {
                        rng.Merge = true;
                        rng.Style.Font.Bold = true;
                        rng.Style.Font.Size = 20;
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    using (ExcelRange rng = workSheet.Cells[2,1,2,6])
                    {
                        rng.Merge = true;
                        rng.Style.Font.Size = 13;
                        rng.Style.Font.Bold = true;
                        rng.Value = "Created at: " + localDate.ToShortTimeString() + " on " + localDate.ToShortDateString();
                        rng.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right; 
                        
                    }
                    try
                    {
                        Byte[] data = excel.GetAsByteArray();
                        //added localDate to the filename so that users can quickly see time and ate run in file name
                        string filename = "Performances-" + localDate + ".xlsx";
                        string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        return File(data, mimeType, filename);
                    }
                    catch (Exception)
                    {
                        return BadRequest("Could not compile and download file");
                    }
                }
            }
            return NotFound("No Data");
        }

        private async Task AddDocumentsAsync(Musician musician, List<IFormFile> theFiles)
        {
            foreach (var f in theFiles)
            {
                if (f != null)
                {
                    string mimeType = f.ContentType;
                    string fileName = Path.GetFileName(f.FileName);
                    long fileLength = f.Length;

                    if (!(fileName == "" || fileLength == 0))
                    {
                        MusicianDocument m = new MusicianDocument();
                        using (var memoryStream = new MemoryStream())
                        {
                            await f.CopyToAsync(memoryStream);
                            m.FileContent.Content = memoryStream.ToArray();
                        }
                        m.MimeType = mimeType;
                        m.FileName = fileName;
                        musician.MusicianDocuments.Add(m);
                    };
                }
            }
        }
        
        private void PopulateAssignedPlaysData(Musician musician)
        {
            //For this to work, you must have Included the Plays 
            //in the Musician
            var allOptions = _context.Instruments;
            var currentOptionIDs = new HashSet<int>(musician.Plays.Select(b => b.InstrumentID));
            var checkBoxes = new List<CheckOptionVM>();
            foreach (var option in allOptions)
            {
                checkBoxes.Add(new CheckOptionVM
                {
                    ID = option.ID,
                    DisplayText = option.Name,
                    Assigned = currentOptionIDs.Contains(option.ID)
                });
            }
            ViewData["PlayOptions"] = checkBoxes;
        }
       
        private void UpdatePlays(string[] selectedOptions, Musician musicianToUpdate)
        {
            if (selectedOptions == null)
            {
                musicianToUpdate.Plays = new List<Play>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var musicianOptionsHS = new HashSet<int>
                (musicianToUpdate.Plays.Select(c => c.InstrumentID));//IDs of the currently selected Plays
            foreach (var option in _context.Instruments)
            {
                if (selectedOptionsHS.Contains(option.ID.ToString())) //It is checked
                {
                    if (!musicianOptionsHS.Contains(option.ID))  //but not currently included
                    {
                        musicianToUpdate.Plays.Add(new Play { MusicianID = musicianToUpdate.ID, InstrumentID = option.ID });
                    }
                }
                else
                {
                    //Checkbox Not checked
                    if (musicianOptionsHS.Contains(option.ID)) //but it is currently in the history - so remove it
                    {
                        Play playToRemove = musicianToUpdate.Plays.SingleOrDefault(c => c.InstrumentID == option.ID);
                        _context.Remove(playToRemove);
                    }
                }
            }
        }

        private SelectList InstrumentList(int? selectedId)
        {
            return new SelectList(_context
                .Instruments
                .OrderBy(m => m.Name), "ID", "Name", selectedId);
        }

        private void PopulateDropDownLists(Musician musician = null)
        {
            ViewData["InstrumentID"] = InstrumentList(musician?.InstrumentID);
        }

        private bool MusicianExists(int id)
        {
          return _context.Musicians.Any(e => e.ID == id);
        }
        
        private async Task AddPicture(Musician musician, IFormFile thePicture)
        {
            //Get the picture and save it with the Patient (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (musician.MusicianPhoto != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            musician.MusicianPhoto.Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            musician.MusicianThumbnail = _context.MusicianThumbnails.Where(p => p.MusicianID == musician.ID).FirstOrDefault();
                            musician.MusicianThumbnail.Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120);
                        }
                        else //No pictures saved so start new
                        {
                            musician.MusicianPhoto = new MusicianPhoto
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            musician.MusicianThumbnail = new MusicianThumbnail
                            {
                                Content = ResizeImage.shrinkImageWebp(pictureArray, 100, 120),
                                MimeType = "image/webp"
                            };
                        }
                    }
                }
            }
        }
    }
}
