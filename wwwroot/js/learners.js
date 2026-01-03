/**
 * LEARNER MANAGEMENT MODULE
 */

// 1. Fetch and Display Learners
async function loadLearners() {
    const tbody = document.getElementById('learnerTableBody');
    if (!tbody) return;

    try {
        const res = await fetch('/api/learner/list-all');
        const data = await res.json();

        if (!res.ok) throw new Error(data.Message || "Server Error");

        const learners = Array.isArray(data) ? data : (data.value || []);

        const userRole = sessionStorage.getItem('userRole') || 'Staff';
        const isAdmin = userRole === 'Administrator';

        tbody.innerHTML = learners.map(s => {
            let genderIcon = '<i class="fas fa-question" style="color:#ccc"></i>';
            if ((s.gender || "").toLowerCase() === 'male') genderIcon = '<i class="fas fa-mars" style="color:#2563eb"></i> Male';
            if ((s.gender || "").toLowerCase() === 'female') genderIcon = '<i class="fas fa-venus" style="color:#db2777"></i> Female';

            // IMPORTANT: Name click triggers viewLearnerDetails
            return `
            <tr>
                <td><span style="font-family:monospace; color:#64748b;">${s.adNo}</span></td>
                <td><a href="#" onclick="viewLearnerDetails(${s.id}); return false;" style="font-weight:bold; color:#0f172a; text-decoration:none; border-bottom:1px dashed #cbd5e1;">${s.name}</a></td>
                <td>${genderIcon}</td> 
                <td><span class="badge" style="background:#f1f5f9; color:#475569;">Gr ${s.grade}</span></td>
                <td>${s.room}</td> 
                <td>
                    ${isAdmin ? `
                        <button class="btn-icon" style="color:#ef4444" onclick="deleteLearner(${s.id})"><i class="fas fa-trash"></i></button>
                        <button class="btn-icon" title="Statement" onclick="viewStatement(${s.id}, '${s.name}')"><i class="fas fa-file-invoice-dollar"></i></button>
                    ` : ''}
                </td>
            </tr>
            `;
        }).join('');
    } catch (err) {
        console.error("Failed to load learners:", err);
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; color:red">Error: ${err.message}</td></tr>`;
    }
}

// 2. VIEW LEARNER DETAILS (THE "PAPER FORM" MODAL)
async function viewLearnerDetails(id) {
    try {
        openModalContent(`
            <div style="text-align:center; padding:50px;">
                <i class="fas fa-spinner fa-spin fa-3x" style="color:#3b82f6;"></i>
                <p style="margin-top:20px; color:#64748b;">Loading Application Form...</p>
            </div>
        `);

        const res = await fetch(`/api/learner/details/${id}`);
        if (!res.ok) throw new Error("Failed to load details");
        const l = await res.json();
        
        const v = (val) => val || "";
        const c = (val) => val ? "checked" : "";
        const d = (dateStr) => {
            if(!dateStr || dateStr.startsWith('0001')) return "";
            return dateStr.split('T')[0];
        };
        const dt = (dateStr) => {
             if(!dateStr || dateStr.startsWith('0001')) return {y:'', m:'', d:''};
             const parts = dateStr.split('T')[0].split('-');
             return { y: parts[0], m: parts[1], d: parts[2] };
        };
        const dob = dt(l.dob);

        const html = `
        <div id="paperFormContainer" class="paper-form">
            <style>
                .paper-form { font-family: "Arial", sans-serif; font-size: 11px; color: #000; max-width: 900px; margin: 0 auto; background: white; }
                .paper-form input[type="text"], .paper-form input[type="date"], .paper-form input[type="number"], .paper-form input[type="email"], .paper-form select, .paper-form textarea {
                    width: 100%; border: none; background: transparent; padding: 2px 5px; font-family: inherit; font-size: inherit; outline: none;
                }
                .paper-form input:focus, .paper-form textarea:focus { background: #f0f9ff; }
                .paper-form table { width: 100%; border-collapse: collapse; border: 1px solid #000; margin-bottom: 5px; }
                .paper-form th, .paper-form td { border: 1px solid #000; padding: 2px 4px; vertical-align: middle; }
                .paper-form th { background: #eee; text-align: left; font-weight: bold; text-transform: uppercase; font-size: 10px; }
                .paper-form .section-header { background: #000; color: #fff; font-weight: bold; padding: 3px 5px; text-transform: uppercase; font-size: 11px; margin-top: 5px; border: 1px solid #000; }
                .paper-form .no-border-bottom { border-bottom: none !important; }
                .paper-form .no-border-top { border-top: none !important; }
                .paper-form .no-border-right { border-right: none !important; }
                .paper-form .no-border-left { border-left: none !important; }
                .paper-form .checkbox-cell { text-align: center; width: 30px; }
                .paper-form input[type="checkbox"] { transform: scale(1.2); }
            </style>

            <div style="border: 2px solid #000; padding: 5px; text-align:center; position:relative; margin-bottom: 5px;">
                <div style="font-weight:bold; font-size: 16px; text-transform:uppercase;">Application For Hostel Accommodation</div>
                <div style="position:absolute; right: 5px; top: 5px; border: 1px solid #000; padding: 2px 5px; width: 100px; text-align:left;">
                    <div style="font-size:9px; border-bottom:1px solid #000;">RECEIPT NO:</div>
                    <div style="text-align:right; font-size:12px;">/26</div>
                </div>
            </div>

            <table class="compact">
                <tr>
                    <td width="15%"><strong>CURRENT GRADE:</strong> <input type="text" value="${v(l.grade)}" style="width:40px; display:inline;"></td>
                    <td width="15%"><strong>GENDER:</strong> ${l.gender}</td>
                    <td width="40%"><strong>SCHOOL OF ENROLMENT:</strong> <input type="text" value="ACADEMIA / HTS" readonly></td>
                    <td><strong>ADM NO:</strong> <input type="text" id="pf_AdmissionNo" value="${v(l.admissionNo)}"></td>
                </tr>
            </table>

            <div class="section-header">LEARNER'S PERSONAL INFORMATION:</div>
            <table>
                <tr>
                    <td width="20%">SURNAME:</td>
                    <td colspan="3"><input type="text" id="pf_Surname" value="${v(l.surname)}"></td>
                    <td rowspan="7" width="20%" style="vertical-align: top; text-align: center; color: #999;">
                        <div style="border: 1px dashed #ccc; height: 100%; display:flex; align-items:center; justify-content:center;">
                            ATTACH PASSPORT PHOTO
                        </div>
                    </td>
                </tr>
                <tr>
                    <td>CHRISTIAN NAMES:</td>
                    <td colspan="3"><input type="text" id="pf_Names" value="${v(l.names)}"></td>
                </tr>
                <tr>
                    <td>PREFERRED NAME:</td>
                    <td><input type="text" id="pf_PreferredName" value="${v(l.preferredName)}"></td>
                    <td width="15%">HOME LANG:</td>
                    <td><input type="text" id="pf_HomeLanguage" value="${v(l.homeLanguage)}"></td>
                </tr>
                <tr>
                    <td>DATE OF BIRTH:</td>
                    <td>
                        Y: <input type="text" value="${dob.y}" style="width:30px; text-align:center;"> 
                        M: <input type="text" value="${dob.m}" style="width:20px; text-align:center;"> 
                        D: <input type="text" value="${dob.d}" style="width:20px; text-align:center;">
                        <input type="date" id="pf_DOB" value="${d(l.dob)}" style="display:none;"> 
                    </td>
                    <td>PRESENT AGE:</td>
                    <td><input type="text" value=""></td>
                </tr>
                <tr>
                    <td>PLACE OF BIRTH:</td>
                    <td><input type="text" id="pf_PlaceOfBirth" value="${v(l.placeOfBirth)}"></td>
                    <td>CITIZENSHIP:</td>
                    <td><input type="text" id="pf_Citizenship" value="${v(l.citizenship)}"></td>
                </tr>
                <tr>
                    <td>STUDY PERMIT NO:</td>
                    <td><input type="text" id="pf_StudyPermitNo" value="${v(l.studyPermitNo)}"></td>
                    <td>CHURCH:</td>
                    <td><input type="text" id="pf_Religion" value="${v(l.religion)}"></td>
                </tr>
                <tr>
                    <td>PRESENT SCHOOL:</td>
                    <td><input type="text" id="pf_PrevSchool" value="${v(l.prevSchool)}"></td>
                    <td>HOSTEL:</td>
                    <td><input type="text" id="pf_PrevHostel" value="${v(l.prevHostel)}"></td>
                </tr>
                <tr>
                    <td>REF TEACHER:</td>
                    <td><input type="text" id="pf_RefTeacher" value="${v(l.refTeacher)}"></td>
                    <td>TEACHER CELL:</td>
                    <td colspan="2"><input type="text" id="pf_RefTeacherCell" value="${v(l.refTeacherCell)}"></td>
                </tr>
                <tr>
                    <td>GRADES REPEATED:</td>
                    <td><input type="text" id="pf_GradesRepeated" value="${v(l.gradesRepeated)}"></td>
                    <td>LEARNER CELL:</td>
                    <td colspan="2"><input type="text" id="pf_LearnerCell" value="${v(l.learnerCell)}"></td>
                </tr>
                <tr>
                    <td>RESIDENTIAL ADDRESS:</td>
                    <td colspan="4"><textarea id="pf_HomeAddress" rows="2">${v(l.homeAddress)}</textarea></td>
                </tr>
            </table>

            <table style="margin-top: -6px; border-top: none;">
                <tr>
                    <td width="20%">SIBLINGS IN HOSTEL:</td>
                    <td width="5%">1.</td> <td width="35%"><input type="text" id="pf_Sib1Name" value="${v(l.sib1Name)}" placeholder="Name"></td> <td width="10%"><input type="text" id="pf_Sib1Grade" value="${v(l.sib1Grade)}" placeholder="Gr"></td>
                    <td width="5%">2.</td> <td width="35%"><input type="text" id="pf_Sib2Name" value="${v(l.sib2Name)}" placeholder="Name"></td> <td width="10%"><input type="text" id="pf_Sib2Grade" value="${v(l.sib2Grade)}" placeholder="Gr"></td>
                </tr>
                <tr>
                    <td width="20%">(NAMES & GRADES)</td>
                    <td width="5%">3.</td> <td width="35%"><input type="text" id="pf_Sib3Name" value="${v(l.sib3Name)}" placeholder="Name"></td> <td width="10%"><input type="text" id="pf_Sib3Grade" value="${v(l.sib3Grade)}" placeholder="Gr"></td>
                    <td width="5%">4.</td> <td width="35%"><input type="text" id="pf_Sib4Name" value="${v(l.sib4Name)}" placeholder="Name"></td> <td width="10%"><input type="text" id="pf_Sib4Grade" value="${v(l.sib4Grade)}" placeholder="Gr"></td>
                </tr>
            </table>

            <div class="section-header">PARENT'S INFORMATION:</div>
            <table>
                <tr>
                    <th width="50%" colspan="2" style="text-align:center;">FATHER / GUARDIAN</th>
                    <th width="50%" colspan="2" style="text-align:center;">MOTHER / GUARDIAN</th>
                </tr>
                <tr><td>SURNAME:</td><td><input type="text" id="pf_FatherName" value="${v(l.fatherName)}"></td> <td>SURNAME:</td><td><input type="text" id="pf_MotherName" value="${v(l.motherName)}"></td></tr>
                <tr><td>ID NO:</td><td><input type="text" id="pf_FatherID" value="${v(l.fatherID)}"></td> <td>ID NO:</td><td><input type="text" id="pf_MotherID" value="${v(l.motherID)}"></td></tr>
                <tr><td>OCCUPATION:</td><td><input type="text" id="pf_FatherOccupation" value="${v(l.fatherOccupation)}"></td> <td>OCCUPATION:</td><td><input type="text" id="pf_MotherOccupation" value="${v(l.motherOccupation)}"></td></tr>
                <tr><td>EMPLOYER:</td><td><input type="text" id="pf_FatherEmployer" value="${v(l.fatherEmployer)}"></td> <td>EMPLOYER:</td><td><input type="text" id="pf_MotherEmployer" value="${v(l.motherEmployer)}"></td></tr>
                <tr><td>TEL (HOME):</td><td><input type="text" id="pf_FatherHomePhone" value="${v(l.fatherHomePhone)}"></td> <td>TEL (HOME):</td><td><input type="text" id="pf_MotherHomePhone" value="${v(l.motherHomePhone)}"></td></tr>
                <tr><td>TEL (WORK):</td><td><input type="text" id="pf_FatherWorkPhone" value="${v(l.fatherWorkPhone)}"></td> <td>TEL (WORK):</td><td><input type="text" id="pf_MotherWorkPhone" value="${v(l.motherWorkPhone)}"></td></tr>
                <tr><td>CELL NO:</td><td><input type="text" id="pf_FatherCell" value="${v(l.fatherCell)}"></td> <td>CELL NO:</td><td><input type="text" id="pf_MotherCell" value="${v(l.motherCell)}"></td></tr>
                <tr><td>EMAIL:</td><td><input type="text" id="pf_FatherEmail" value="${v(l.fatherEmail)}"></td> <td>EMAIL:</td><td><input type="text" id="pf_MotherEmail" value="${v(l.motherEmail)}"></td></tr>
                <tr><td>RES ADDRESS:</td><td><textarea id="pf_FatherResAddress" rows="2">${v(l.fatherResAddress)}</textarea></td> <td>RES ADDRESS:</td><td><textarea id="pf_MotherResAddress" rows="2">${v(l.motherResAddress)}</textarea></td></tr>
                <tr><td>POST ADDRESS:</td><td><textarea id="pf_FatherPostalAddress" rows="2">${v(l.fatherPostalAddress)}</textarea></td> <td>POST ADDRESS:</td><td><textarea id="pf_MotherPostalAddress" rows="2">${v(l.motherPostalAddress)}</textarea></td></tr>
            </table>

            <div class="section-header">LIST OF RELATIVES (OLDER THAN 21) PERMITTED TO SIGN LEARNER OUT:</div>
            <table>
                <tr><th width="50%">NAME</th><th width="25%">ID NO.</th><th width="25%">TEL NO.</th></tr>
                <tr><td><input type="text" id="pf_Rel1Name" value="${v(l.rel1Name)}"></td> <td><input type="text" id="pf_Rel1ID" value="${v(l.rel1ID)}"></td> <td><input type="text" id="pf_Rel1Tel" value="${v(l.rel1Tel)}"></td></tr>
                <tr><td><input type="text" id="pf_Rel2Name" value="${v(l.rel2Name)}"></td> <td><input type="text" id="pf_Rel2ID" value="${v(l.rel2ID)}"></td> <td><input type="text" id="pf_Rel2Tel" value="${v(l.rel2Tel)}"></td></tr>
                <tr><td><input type="text" id="pf_Rel3Name" value="${v(l.rel3Name)}"></td> <td><input type="text" id="pf_Rel3ID" value="${v(l.rel3ID)}"></td> <td><input type="text" id="pf_Rel3Tel" value="${v(l.rel3Tel)}"></td></tr>
                <tr><td><input type="text" id="pf_Rel4Name" value="${v(l.rel4Name)}"></td> <td><input type="text" id="pf_Rel4ID" value="${v(l.rel4ID)}"></td> <td><input type="text" id="pf_Rel4Tel" value="${v(l.rel4Tel)}"></td></tr>
                <tr><td><input type="text" id="pf_Rel5Name" value="${v(l.rel5Name)}"></td> <td><input type="text" id="pf_Rel5ID" value="${v(l.rel5ID)}"></td> <td><input type="text" id="pf_Rel5Tel" value="${v(l.rel5Tel)}"></td></tr>
                <tr><td><input type="text" id="pf_Rel6Name" value="${v(l.rel6Name)}"></td> <td><input type="text" id="pf_Rel6ID" value="${v(l.rel6ID)}"></td> <td><input type="text" id="pf_Rel6Tel" value="${v(l.rel6Tel)}"></td></tr>
            </table>

            <div class="section-header">LEARNER'S MEDICAL INFORMATION:</div>
            <table>
                <tr>
                    <td width="15%">MEDICAL AID:</td> <td><input type="text" id="pf_MedicalAidName" value="${v(l.medicalAidName)}"></td>
                    <td width="10%">NUMBER:</td> <td><input type="text" id="pf_MedicalAidNo" value="${v(l.medicalAidNo)}"></td>
                    <td width="15%">MAIN MEMBER:</td> <td><input type="text" id="pf_MedicalMainMember" value="${v(l.medicalMainMember)}"></td>
                </tr>
                <tr>
                    <td>AMBULANCE:</td> <td colspan="2">1: <input type="text" id="pf_AmbChoice1" value="${v(l.ambChoice1)}" style="width:80%"></td> <td colspan="2">2: <input type="text" id="pf_AmbChoice2" value="${v(l.ambChoice2)}" style="width:80%"></td>
                </tr>
                <tr>
                    <td>HOSPITAL:</td> <td colspan="2">1: <input type="text" id="pf_HospChoice1" value="${v(l.hospChoice1)}" style="width:80%"></td> <td colspan="2">2: <input type="text" id="pf_HospChoice2" value="${v(l.hospChoice2)}" style="width:80%"></td>
                </tr>
            </table>

            <div class="section-header">MEDICAL HISTORY (COMPLETED BY DOCTOR OR CLINIC):</div>
            <table>
                <tr>
                    <td width="15%">BLOOD GROUP:</td> <td width="15%"><input type="text" id="pf_BloodGroup" value="${v(l.bloodGroup)}"></td>
                    <td width="15%">CHRONIC MEDS:</td> <td><input type="text" id="pf_ChronicMedication" value="${v(l.chronicMedication)}"></td>
                </tr>
                <tr>
                    <td style="vertical-align:top;">MEDICAL HISTORY:<br><span style="font-size:9px;">(Allergies, Epilepsy, Operations, etc)</span></td>
                    <td colspan="3"><textarea id="pf_MedicalHistory" rows="3">${v(l.medicalHistory)}</textarea></td>
                </tr>
            </table>

            <div style="border:1px solid #000; padding:5px; margin-top:5px; display:flex; justify-content:space-between; align-items:center;">
                <div>
                    HEREWITH I, DR. <input type="text" id="pf_DoctorName" value="${v(l.doctorName)}" style="width:150px; border-bottom:1px dotted #000;">
                    DECLARE THAT THE LEARNER IS: 
                    <select id="pf_DoctorDeclaredFit" style="width:80px; border:1px solid #ccc;">
                        <option value="true" ${l.doctorDeclaredFit ? 'selected' : ''}>FIT</option>
                        <option value="false" ${!l.doctorDeclaredFit ? 'selected' : ''}>UNFIT</option>
                    </select> 
                    TO RESIDE IN A HOSTEL.
                </div>
                <div style="text-align:center; border:1px solid #ccc; width:100px; height:50px; color:#ccc; display:flex; align-items:center; justify-content:center; font-size:9px;">
                    STAMP
                </div>
            </div>

            <div class="section-header">DOCUMENTS TO BE ATTACHED:</div>
            <table>
                <tr><th colspan="2">DOCUMENT DESCRIPTION</th> <th width="10%" style="text-align:center;">CHECK</th></tr>
                <tr><td width="5%">1.</td> <td>Recent passport photo</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocPassportPhoto" ${c(l.docPassportPhoto)}></td></tr>
                <tr><td>2.</td> <td>Certified copy of birth certificate</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocBirthCert" ${c(l.docBirthCert)}></td></tr>
                <tr><td>3.</td> <td>Certified copy of June report card</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocReportJune" ${c(l.docReportJune)}></td></tr>
                <tr><td>4.</td> <td>Certified copy of Dec report card (Prev Year)</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocReportDec" ${c(l.docReportDec)}></td></tr>
                <tr><td>5.</td> <td>Certified copies of Parents/Guardians ID</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocParentsID" ${c(l.docParentsID)}></td></tr>
                <tr><td>6.</td> <td>Proof of Res Address (Municipal Acc)</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocMunicipal" ${c(l.docMunicipal)}></td></tr>
                <tr><td>7.</td> <td>Proof of Employment / Payslip</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocEmployer" ${c(l.docEmployer)}></td></tr>
                <tr><td>8.</td> <td>Doctor's Declaration</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocDoctorDecl" ${c(l.docDoctorDecl)}></td></tr>
                <tr><td>9.</td> <td>Proof of School Acceptance</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocProofAccept" ${c(l.docProofAccept)}></td></tr>
                <tr><td>10.</td> <td>Study Permit (If applicable)</td> <td class="checkbox-cell"><input type="checkbox" id="pf_DocStudyPermit" ${c(l.docStudyPermit)}></td></tr>
            </table>

            <div style="margin-top: 20px; text-align: right; padding-bottom: 20px;">
                <button onclick="savePaperForm(${l.learnerID})" style="background: #2563eb; color: white; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-size: 14px; font-weight: bold;">
                    <i class="fas fa-save"></i> UPDATE & SAVE FORM
                </button>
                <button onclick="document.getElementById('dynamicModal').remove()" style="background: #64748b; color: white; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; font-size: 14px; margin-left: 10px;">
                    CLOSE
                </button>
            </div>
        </div>
        `;

        const existing = document.getElementById('dynamicModal');
        if (existing) existing.remove();
        openModalContent(html);

    } catch (err) {
        console.error("Details Error:", err);
        alert("Could not load details: " + err.message);
    }
}

async function savePaperForm(id) {
    const btn = event.target;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> SAVING...';
    btn.disabled = true;

    try {
        const getVal = (id) => document.getElementById(id)?.value || "";
        const getChk = (id) => document.getElementById(id)?.checked || false;

        const data = {
            LearnerID: id,
            AdmissionNo: getVal('pf_AdmissionNo'),
            Surname: getVal('pf_Surname'),
            Names: getVal('pf_Names'),
            PreferredName: getVal('pf_PreferredName'),
            HomeLanguage: getVal('pf_HomeLanguage'),
            PlaceOfBirth: getVal('pf_PlaceOfBirth'),
            Citizenship: getVal('pf_Citizenship'),
            StudyPermitNo: getVal('pf_StudyPermitNo'),
            Religion: getVal('pf_Religion'),
            LearnerCell: getVal('pf_LearnerCell'),
            HomeAddress: getVal('pf_HomeAddress'),
            DOB: getVal('pf_DOB'), 
            PrevSchool: getVal('pf_PrevSchool'),
            PrevHostel: getVal('pf_PrevHostel'),
            RefTeacher: getVal('pf_RefTeacher'),
            RefTeacherCell: getVal('pf_RefTeacherCell'),
            GradesRepeated: getVal('pf_GradesRepeated'),
            Sib1Name: getVal('pf_Sib1Name'), Sib1Grade: getVal('pf_Sib1Grade'),
            Sib2Name: getVal('pf_Sib2Name'), Sib2Grade: getVal('pf_Sib2Grade'),
            Sib3Name: getVal('pf_Sib3Name'), Sib3Grade: getVal('pf_Sib3Grade'),
            Sib4Name: getVal('pf_Sib4Name'), Sib4Grade: getVal('pf_Sib4Grade'),
            FatherName: getVal('pf_FatherName'), FatherID: getVal('pf_FatherID'), FatherOccupation: getVal('pf_FatherOccupation'),
            FatherEmployer: getVal('pf_FatherEmployer'), FatherHomePhone: getVal('pf_FatherHomePhone'), FatherWorkPhone: getVal('pf_FatherWorkPhone'),
            FatherCell: getVal('pf_FatherCell'), FatherEmail: getVal('pf_FatherEmail'), FatherResAddress: getVal('pf_FatherResAddress'), FatherPostalAddress: getVal('pf_FatherPostalAddress'),
            MotherName: getVal('pf_MotherName'), MotherID: getVal('pf_MotherID'), MotherOccupation: getVal('pf_MotherOccupation'),
            MotherEmployer: getVal('pf_MotherEmployer'), MotherHomePhone: getVal('pf_MotherHomePhone'), MotherWorkPhone: getVal('pf_MotherWorkPhone'),
            MotherCell: getVal('pf_MotherCell'), MotherEmail: getVal('pf_MotherEmail'), MotherResAddress: getVal('pf_MotherResAddress'), MotherPostalAddress: getVal('pf_MotherPostalAddress'),
            Rel1Name: getVal('pf_Rel1Name'), Rel1ID: getVal('pf_Rel1ID'), Rel1Tel: getVal('pf_Rel1Tel'),
            Rel2Name: getVal('pf_Rel2Name'), Rel2ID: getVal('pf_Rel2ID'), Rel2Tel: getVal('pf_Rel2Tel'),
            Rel3Name: getVal('pf_Rel3Name'), Rel3ID: getVal('pf_Rel3ID'), Rel3Tel: getVal('pf_Rel3Tel'),
            Rel4Name: getVal('pf_Rel4Name'), Rel4ID: getVal('pf_Rel4ID'), Rel4Tel: getVal('pf_Rel4Tel'),
            Rel5Name: getVal('pf_Rel5Name'), Rel5ID: getVal('pf_Rel5ID'), Rel5Tel: getVal('pf_Rel5Tel'),
            Rel6Name: getVal('pf_Rel6Name'), Rel6ID: getVal('pf_Rel6ID'), Rel6Tel: getVal('pf_Rel6Tel'),
            MedicalAidName: getVal('pf_MedicalAidName'), MedicalAidNo: getVal('pf_MedicalAidNo'), MedicalMainMember: getVal('pf_MedicalMainMember'),
            AmbChoice1: getVal('pf_AmbChoice1'), AmbChoice2: getVal('pf_AmbChoice2'), HospChoice1: getVal('pf_HospChoice1'), HospChoice2: getVal('pf_HospChoice2'),
            BloodGroup: getVal('pf_BloodGroup'), ChronicMedication: getVal('pf_ChronicMedication'), MedicalHistory: getVal('pf_MedicalHistory'),
            DoctorName: getVal('pf_DoctorName'), DoctorDeclaredFit: document.getElementById('pf_DoctorDeclaredFit').value === 'true',
            DocPassportPhoto: getChk('pf_DocPassportPhoto'), DocBirthCert: getChk('pf_DocBirthCert'), DocReportJune: getChk('pf_DocReportJune'),
            DocReportDec: getChk('pf_DocReportDec'), DocParentsID: getChk('pf_DocParentsID'), DocMunicipal: getChk('pf_DocMunicipal'),
            DocEmployer: getChk('pf_DocEmployer'), DocDoctorDecl: getChk('pf_DocDoctorDecl'), DocProofAccept: getChk('pf_DocProofAccept'), DocStudyPermit: getChk('pf_DocStudyPermit')
        };

        const res = await fetch(`/api/learner/update/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });

        if (res.ok) {
            alert("Form Saved Successfully!");
            document.getElementById('dynamicModal').remove();
            loadLearners(); 
        } else {
            const err = await res.json();
            alert("Error saving: " + err.Message);
        }
    } catch (e) {
        alert("Network Error: " + e.message);
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

// RESTORED HELPERS

async function loadRoomsForGender() {
    const genderSelect = document.getElementById('lnGender');
    const roomSelect = document.getElementById('regRoomSelect');
    const gender = genderSelect.value;

    roomSelect.innerHTML = '<option value="">Loading...</option>';

    if (!gender) {
        roomSelect.innerHTML = '<option value="">-- Select Gender First --</option>';
        return;
    }

    try {
        const res = await fetch(`/api/learner/rooms/available?gender=${gender}`);
        const rooms = await res.json();

        if (rooms.length === 0) {
            roomSelect.innerHTML = '<option value="">No beds available for this gender!</option>';
        } else {
            roomSelect.innerHTML = rooms.map(r =>
                `<option value="${r.id}">${r.name}</option>`
            ).join('');
        }
    } catch (err) {
        console.error(err);
        roomSelect.innerHTML = '<option value="">Error loading rooms</option>';
    }
}

async function saveLearner() {
    const formData = {
        Surname: document.getElementById('lnSurname').value,
        Names: document.getElementById('lnNames').value,
        Gender: document.getElementById('lnGender').value,
        RoomId: parseInt(document.getElementById('regRoomSelect').value),
        AdmissionNo: "TBD",
        PreferredName: document.getElementById('lnPref').value,
        Grade: parseInt(document.getElementById('lnGrade').value) || 8,
        HomeLanguage: document.getElementById('lnLang').value,
        DOB: document.getElementById('lnDOB').value,
        PlaceOfBirth: document.getElementById('lnPOB').value,
        Citizenship: document.getElementById('lnCitizen').value,
        StudyPermitNo: document.getElementById('lnPermit').value,
        HomeAddress: document.getElementById('lnAddress').value,
        PrevSchool: document.getElementById('lnPrevSchool').value,
        PrevHostel: document.getElementById('lnPrevHostel').value,
        RefTeacher: document.getElementById('lnRef').value,
        RefTeacherCell: document.getElementById('lnRefCell').value,
        GradesRepeated: document.getElementById('lnRepeated').value,
        FatherName: document.getElementById('pFatherName').value,
        FatherID: document.getElementById('pFatherID').value,
        FatherEmployer: document.getElementById('pFatherEmp').value,
        FatherPhone: document.getElementById('pFatherPhone').value,
        FatherEmail: document.getElementById('pFatherEmail').value,
        MotherName: document.getElementById('pMotherName').value,
        MotherID: document.getElementById('pMotherID').value,
        MotherEmployer: document.getElementById('pMotherEmp').value,
        MotherPhone: document.getElementById('pMotherPhone').value,
        MotherEmail: document.getElementById('pMotherEmail').value,
        MedicalAidName: document.getElementById('medAidName').value,
        MedicalAidNo: document.getElementById('medAidNo').value,
        DoctorName: document.getElementById('medDoctor').value,
        MedicalConditions: document.getElementById('medHistory').value,
        EmergencyContact: document.getElementById('medRelatives').value
    };

    if (!formData.Gender || !formData.RoomId) {
        alert("Please select Gender and Room.");
        return;
    }

    try {
        const res = await fetch('/api/learner/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });

        if (res.ok) {
            alert("Application Submitted Successfully!");
            closeModal('regModal');
            loadLearners();
        } else {
            const err = await res.json();
            alert("Error: " + err.Message);
        }
    } catch (err) { alert("Failed to save: " + err.message); }
}

function openRegModal() {
    document.getElementById('editLearnerId').value = "";
    document.getElementById('learnerForm').reset();
    document.getElementById('regModal').style.display = 'flex';
}

function openModal(id) { document.getElementById(id).style.display = 'flex'; }
function closeModal(id) { document.getElementById(id).style.display = 'none'; }

async function deleteLearner(id) {
    if (!confirm("Are you sure? This will also update the room occupancy.")) return;
    try {
        const res = await fetch(`/api/learner/delete/${id}`, { method: 'DELETE' });
        if (res.ok) { loadLearners(); if (window.updateStats) updateStats(); }
    } catch (err) { console.error("Delete failed:", err); }
}

function openModalContent(html) {
    const modal = document.createElement('div');
    modal.className = 'modal-overlay';
    modal.id = 'dynamicModal';
    modal.style.display = 'flex';
    modal.style.zIndex = '1000';
    modal.innerHTML = `<div class="modal-content" style="max-width:800px; width:90%;">${html}</div>`;
    document.body.appendChild(modal);
}

function syncFormLogo() {
    const mainImg = document.getElementById('hostelLogoImg');
    const mainName = document.getElementById('hostelNameDisplay');
    const formImg = document.getElementById('formLogoImg');
    const formPlaceholder = document.getElementById('formLogoPlaceholder');
    const formTitle = document.querySelector('#regModal .header-center h2');

    if (mainImg && mainImg.src && mainImg.style.display !== 'none') {
        formImg.src = mainImg.src;
        formImg.style.display = 'block';
        if (formPlaceholder) formPlaceholder.style.display = 'none';
    } else {
        if (formImg) formImg.style.display = 'none';
        if (formPlaceholder) formPlaceholder.style.display = 'flex';
    }

    if (mainName && formTitle && mainName.innerText !== "HostelPro") {
        formTitle.innerText = mainName.innerText.toUpperCase();
    }
}

// RESTORED FINANCIAL HELPERS

function downloadCSV(transactions, studentName) {
    let csvContent = "data:text/csv;charset=utf-8,";
    csvContent += "Date,Description,Receipt No,Amount\n";
    transactions.forEach(function (row) {
        let rowStr = `${row.date},"${row.description}",${row.receipt},${row.amount}`;
        csvContent += rowStr + "\n";
    });
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    const fileName = `Statement_${studentName.replace(/ /g, "_")}.csv`;
    link.setAttribute("download", fileName);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

async function viewStatement(learnerId, learnerName) {
    try {
        openModalContent(`
            <div style="text-align:center; padding:40px;">
                <i class="fas fa-spinner fa-spin fa-2x"></i>
                <p>Generating Statement for ${learnerName}...</p>
            </div>
        `);

        const res = await fetch(`/api/Financials/statement/${learnerId}`);
        if (!res.ok) throw new Error("Failed to load statement");
        const data = await res.json();
        const fmt = (num) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'NAD' }).format(num);

        const html = `
            <div style="display:flex; justify-content:space-between; align-items:start; margin-bottom:20px; border-bottom: 2px solid #eee; padding-bottom:15px;">
                <div>
                    <h2 style="margin:0; color:#1f2937;">Financial Statement</h2>
                    <p style="margin:5px 0 0 0; color:#6b7280;">${data.generatedDate}</p>
                </div>
                <div style="text-align:right;">
                    <h3 style="margin:0; color:#111827;">${data.learner}</h3>
                    <p style="margin:0; color:#6b7280;">Ref: ${data.admissionNo}</p>
                </div>
            </div>
            <div class="table-container" style="max-height: 400px; overflow-y: auto;">
                <table style="width:100%; border-collapse: collapse;">
                    <thead style="background:#f9fafb; position: sticky; top: 0;">
                        <tr>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Date</th>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Description</th>
                            <th style="text-align:left; padding:12px; border-bottom:2px solid #e5e7eb;">Receipt #</th>
                            <th style="text-align:right; padding:12px; border-bottom:2px solid #e5e7eb;">Amount</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.transactions.length > 0 ? data.transactions.map(t => `
                            <tr>
                                <td style="padding:12px; border-bottom:1px solid #eee;">${t.date}</td>
                                <td style="padding:12px; border-bottom:1px solid #eee;">${t.description}</td>
                                <td style="padding:12px; border-bottom:1px solid #eee;">
                                    <span style="background:#e0f2fe; color:#0369a1; padding:2px 8px; border-radius:12px; font-size:0.85em;">${t.receipt}</span>
                                </td>
                                <td style="text-align:right; padding:12px; border-bottom:1px solid #eee; font-weight:bold;">${fmt(t.amount)}</td>
                            </tr>
                        `).join('') : `<tr><td colspan="4" style="text-align:center; padding:20px; color:#999;">No transactions found.</td></tr>`}
                    </tbody>
                </table>
            </div>
            <div style="display:flex; justify-content:flex-end; margin-top:20px; padding-top:15px; border-top: 2px solid #eee;">
                <div style="text-align:right;">
                    <span style="display:block; font-size:0.9em; color:#6b7280;">Total Paid</span>
                    <span style="display:block; font-size:1.5em; font-weight:bold; color:#059669;">${fmt(data.totalPaid)}</span>
                </div>
            </div>
            <div style="margin-top:20px; text-align:right;">
                <button onclick="document.getElementById('dynamicModal').remove()" style="padding:10px 20px; background:#6b7280; color:white; border:none; border-radius:6px; cursor:pointer;">Close</button>
                <button id="btnExport" style="padding:10px 20px; background:#10b981; color:white; border:none; border-radius:6px; cursor:pointer; margin-left:10px;">
                    <i class="fas fa-file-excel"></i> Export CSV
                </button>
                <button onclick="window.print()" style="padding:10px 20px; background:#0ea5e9; color:white; border:none; border-radius:6px; cursor:pointer; margin-left:10px;">
                    <i class="fas fa-print"></i> Print
                </button>
            </div>
        `;

        const existing = document.getElementById('dynamicModal');
        if (existing) existing.remove();
        openModalContent(html);

        document.getElementById('btnExport').onclick = function () {
            downloadCSV(data.transactions, learnerName);
        };

    } catch (err) {
        console.error("Statement Error:", err);
        const existing = document.getElementById('dynamicModal');
        if (existing) existing.remove();
        alert("Could not load statement: " + err.message);
    }
}
